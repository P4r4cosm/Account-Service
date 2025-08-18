using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace AccountService.Infrastructure.Persistence.MessageBroker;

public abstract class RabbitMqConsumerBase : BackgroundService
{
    // Зависимости и конфигурация
    private readonly ILogger<RabbitMqConsumerBase> _logger;
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly AsyncRetryPolicy _retryPolicy;
    private IChannel? _channel;

    // Свойства, определяемые наследниками
    protected abstract string QueueName { get; }
    protected abstract string HandlerName { get; }

    protected RabbitMqConsumerBase(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<RabbitMqConsumerBase> logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;

        _retryPolicy = Policy
            // 1. Указываем, какие исключения считать временными (transient)
            .Handle<DbUpdateException>() // Например, deadlock или transient-ошибка сети с БД
            .Or<NpgsqlException>(ex => ex.IsTransient) // Явно ловим временные ошибки PostgreSQL
            .Or<TimeoutException>() // Таймаут при обращении к внешнему ресурсу
            // 2. Настраиваем стратегию ожидания и повтора
            .WaitAndRetryAsync(
                3, // Количество повторных попыток
                retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) // Экспоненциальная выдержка: 2, 4, 8 секунд
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)), // Джиттер для сглаживания пиков нагрузки
                (exception, timespan, retryCount, context) =>
                {
                    // 3. Логируем каждую попытку, используя контекст
                    var eventId = context.Contains("EventId") ? context["EventId"] : "N/A";
                    _logger.LogWarning(exception,
                        "[EventId: {EventId}] Попытка #{RetryCount} из {MaxRetries}. Повтор через {Delay:F0} мс. Причина: {ExceptionMessage}",
                        eventId, retryCount, 3, timespan.TotalMilliseconds, exception.Message);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{HandlerName} запускается.", HandlerName);
        stoppingToken.Register(() => _logger.LogInformation("{HandlerName} останавливается.", HandlerName));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndConsume(stoppingToken);
                // Если мы вышли отсюда без исключения, значит была штатная отмена
                break;
            }
            catch (OperationCanceledException)
            {
                // Это ожидаемое исключение при остановке сервиса. Просто выходим из цикла.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex,
                    "Критическая ошибка в потребителе {HandlerName}. Попытка переподключения через 5 секунд.",
                    HandlerName);
                // Ждем перед попыткой пересоздать соединение и потребителя
                await Task.Delay(5000, stoppingToken);
            }
        }

        _logger.LogInformation("{HandlerName} завершил работу.", HandlerName);
    }

    private async Task ConnectAndConsume(CancellationToken cancellationToken)
    {
        if (!_connection.IsOpen)
        {
            _logger.LogWarning("Соединение с RabbitMQ закрыто. Ожидание перед подключением...");
            // Можно добавить политику Polly и сюда, для ожидания доступности RabbitMQ при старте
            throw new BrokerUnreachableException(
                new Exception("Соединение недоступно при попытке запуска потребителя."));
        }

        await using (_channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken))
        {
            await _channel.BasicQosAsync(0, 1, false, cancellationToken);
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceived;

            // Подписываемся на событие закрытия канала, чтобы уронить весь consumer
            _channel.CallbackExceptionAsync += (_, ea) =>
            {
                _logger.LogError(ea.Exception, "Произошла ошибка в канале RabbitMQ. Consumer будет перезапущен.");
                // Отписываемся, чтобы избежать повторного вызова
                consumer.ReceivedAsync -= OnMessageReceived;
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken);
            _logger.LogInformation("{HandlerName}: начато прослушивание очереди '{QueueName}'.", HandlerName,
                QueueName);

            // Ждем отмены, пока consumer работает
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        var stopwatch = Stopwatch.StartNew();
        var messageBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        
        if (!TryValidateEnvelope(messageBody, out var eventId))
        {
            await HandleInvalidMessageAsync(eventArgs.DeliveryTag, messageBody, eventId,
                "Невалидная оболочка или версия");
            return;
        }

        // Добавляем чтение CausationId из кастомных заголовков
        var causationId = Guid.Empty;
        if (eventArgs.BasicProperties.Headers?.TryGetValue("X-Causation-Id", out var causationIdObj) == true &&
            Guid.TryParse(causationIdObj?.ToString(), out var parsedCausationId))
        {
            causationId = parsedCausationId;
        }

        if (!Guid.TryParse(eventArgs.BasicProperties.CorrelationId, out var correlationId))
        {
            _logger.LogWarning("CorrelationId отсутствует или имеет неверный формат: '{CorrelationId}'",
                eventArgs.BasicProperties.CorrelationId);
            correlationId = Guid.NewGuid();
        }

        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["EventId"] = eventId,
                   ["CorrelationId"] = correlationId,
                   ["CausationId"] = causationId,
                   ["MessageType"] = eventArgs.RoutingKey
               }))
        {
            try
            {
                if (await IsMessageAlreadyHandled(eventId))
                {
                    _logger.LogInformation("Сообщение уже было обработано (быстрая проверка). Пропуск.");
                    await AcknowledgeMessageAsync(eventArgs.DeliveryTag);
                    return;
                }

                await ProcessMessageWithRetriesAsync(eventId, eventArgs.RoutingKey, messageBody);

                stopwatch.Stop();
                _logger.LogInformation("Сообщение успешно обработано. Затрачено: {LatencyMs} мс.",
                    stopwatch.ElapsedMilliseconds);
                await AcknowledgeMessageAsync(eventArgs.DeliveryTag);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Не удалось обработать сообщение после всех попыток. Перемещение в карантин. Затрачено: {LatencyMs} мс.",
                    stopwatch.ElapsedMilliseconds);
                await HandleFinalFailureAsync(eventArgs.DeliveryTag, messageBody, eventId, ex.Message);
            }
        }
    }

    private async Task HandleInvalidMessageAsync(ulong deliveryTag, string payload, Guid? messageId, string error)
    {
        _logger.LogWarning("{Error}. Перемещение в карантин. Тело: {MessageBody}", error, payload);
        await HandleDeadLetterAsync(null, error, messageId ?? Guid.NewGuid(), payload);
        await AcknowledgeMessageAsync(deliveryTag);
    }

    private async Task HandleFinalFailureAsync(ulong deliveryTag, string payload, Guid messageId, string error)
    {
        await HandleDeadLetterAsync(null, $"Не удалось обработать после всех попыток: {error}", messageId, payload);
        // Используем NACK, чтобы сообщить брокеру о неудаче (если настроен DLX, сообщение уйдет туда)
        if (_channel is not null) await _channel.BasicNackAsync(deliveryTag, false, false);
    }

    private static bool TryValidateEnvelope(string payload, out Guid eventId)
    {
        eventId = Guid.Empty;
        try
        {
            var envelope = JsonSerializer.Deserialize<EventEnvelope<object>>(payload);
            if (envelope is null || envelope.EventId == Guid.Empty) return false;

            eventId = envelope.EventId;
            var version = envelope.Meta.Version;
            return version == "v1"; // Проверяем версию сразу
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<bool> IsMessageAlreadyHandled(Guid eventId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var inbox = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
            return await inbox.IsHandledAsync(eventId, HandlerName, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при предварительной проверке идемпотентности. Обработка будет продолжена.");
            return false;
        }
    }

    private async Task ProcessMessageWithRetriesAsync(Guid eventId, string routingKey, string messageBody)
    {
        var policyContext = new Context($"Event-{eventId}", new Dictionary<string, object> { { "EventId", eventId } });

        await _retryPolicy.ExecuteAsync(async _ =>
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

            await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);

            if (await inboxRepository.IsHandledAsync(eventId, HandlerName, CancellationToken.None))
            {
                await unitOfWork.RollbackTransactionAsync();
                _logger.LogInformation("Сообщение уже было обработано (проверено в транзакции). Пропуск.");
                return;
            }

            await ProcessMessageAsync(scope, routingKey, messageBody);

            inboxRepository.Add(new InboxConsumedMessage
                { MessageId = eventId, Handler = HandlerName, ProcessedAt = DateTime.UtcNow });

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();
        }, policyContext);
    }

    private async Task HandleDeadLetterAsync(IServiceScope? scope, string error, Guid messageId, string payload)
    {
        // Если scope не передан, создаем его локально
        if (scope is null)
        {
            using var localScope = _serviceProvider.CreateScope();
            await WriteToDeadLetter(localScope, error, messageId, payload);
        }
        else
        {
            await WriteToDeadLetter(scope, error, messageId, payload);
        }
    }

    private async Task WriteToDeadLetter(IServiceScope scope, string error, Guid messageId, string payload)
    {
        try
        {
            var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            inboxRepository.AddToDeadLetter(new InboxDeadLetterMessage
            {
                MessageId = messageId,
                ReceivedAt = DateTime.UtcNow,
                Handler = HandlerName,
                Payload = payload,
                Error = error
            });
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "[EventId: {EventId}] Критическая ошибка при записи в таблицу карантина (inbox_dead_letters)!",
                messageId);
        }
    }

    private async Task AcknowledgeMessageAsync(ulong deliveryTag)
    {
        if (_channel is not null) await _channel.BasicAckAsync(deliveryTag, false);
    }
    

    protected abstract Task ProcessMessageAsync(IServiceScope scope, string routingKey, string messageBody);
}