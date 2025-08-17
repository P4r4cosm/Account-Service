using System.Data;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{HandlerName} запускается.", HandlerName);
        cancellationToken.Register(() => _logger.LogInformation("{HandlerName} останавливается.", HandlerName));

        try
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.BasicQosAsync(0, 1, false, cancellationToken);
            _logger.LogInformation("{HandlerName}: канал создан, QoS (prefetch) установлен в 1.", HandlerName);
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceived;

            await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken);
            _logger.LogInformation("{HandlerName}: начато прослушивание очереди '{QueueName}'.", HandlerName,
                QueueName);


            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("{HandlerName}: выполнение было отменено.", HandlerName);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Критическая ошибка в {HandlerName}. Потребитель будет остановлен.", HandlerName);
        }
        finally
        {
            if (_channel is { IsClosed: true })
            {
                await _channel.CloseAsync(
                    cancellationToken:
                    cancellationToken); // Используем Close без токена в finally, т.к. токен уже может быть отменен
            }

            _logger.LogInformation("{HandlerName} завершил работу.", HandlerName);
        }
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        var messageBody = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        var stopwatch = Stopwatch.StartNew();

        // Шаг 1: Десериализация и валидация конверта (Envelope)
        EventEnvelope<object>? genericEnvelope;
        Guid eventId;
        try
        {
            genericEnvelope = JsonSerializer.Deserialize<EventEnvelope<object>>(messageBody);
            if (genericEnvelope is null || (eventId = genericEnvelope.EventId) == Guid.Empty)
            {
                _logger.LogWarning(
                    "Получено сообщение с невалидной оболочкой или пустым eventId. Перемещение в карантин. Тело: {MessageBody}",
                    messageBody);
                await HandleDeadLetterAsync(null, "Невалидная оболочка или пустой eventId", Guid.NewGuid(),
                    messageBody);
                await AcknowledgeMessageAsync(eventArgs.DeliveryTag);
                return;
            }
        }
        catch (JsonException jsonEx)
        {
            _logger.LogWarning(jsonEx,
                "Ошибка десериализации тела сообщения (невалидный JSON). Перемещение в карантин. Тело: {MessageBody}",
                messageBody);
            await HandleDeadLetterAsync(null, "Невалидный формат JSON", Guid.NewGuid(), messageBody);
            await AcknowledgeMessageAsync(eventArgs.DeliveryTag);
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

        // Шаг 2: Настройка контекста для структурированного логирования
        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["EventId"] = eventId,
                   ["CorrelationId"] = correlationId,
                   ["CausationId"] = causationId,
                   ["MessageType"] = eventArgs.RoutingKey
               }))
        {
            // Шаг 3: Валидация метаданных
            if (genericEnvelope.Meta.Version != "v1")
            {
                var error = $"Неподдерживаемая версия: '{genericEnvelope.Meta.Version}'";
                _logger.LogWarning("{Error}. Перемещение в карантин.", error);
                await HandleDeadLetterAsync(null, error, eventId, messageBody);
                await AcknowledgeMessageAsync(eventArgs.DeliveryTag);
                return;
            }

            try
            {
                using var initialScope = _serviceProvider.CreateScope();
                var inboxRepository = initialScope.ServiceProvider.GetRequiredService<IInboxRepository>();
                if (await inboxRepository.IsHandledAsync(eventId, HandlerName, CancellationToken.None))
                {
                    _logger.LogInformation("Сообщение уже было обработано. Пропуск выполнения.");
                    await AcknowledgeMessageAsync(eventArgs.DeliveryTag);
                    return; // Полный выход
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ошибка при предварительной проверке идемпотентности. Сообщение будет обработано с retry.");
            }

            _logger.LogInformation("Начата обработка сообщения.");

            try
            {
                var policyContext = new Context($"Event-{eventId}",
                    new Dictionary<string, object> { { "EventId", eventId } });

                await _retryPolicy.ExecuteAsync(async _ =>
                {
                    using var scope = _serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var inboxRepository = scope.ServiceProvider.GetRequiredService<IInboxRepository>();

                    await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted);

                    // Пессимистичная проверка внутри транзакции
                    if (await inboxRepository.IsHandledAsync(eventId, HandlerName, CancellationToken.None))
                    {
                        await unitOfWork.RollbackTransactionAsync();
                        // Было: "Message already handled..."
                        _logger.LogInformation(
                            "Сообщение уже было обработано (проверено в транзакции). Пропуск выполнения.");
                        return; // Выходим только из лямбды ExecuteAsync
                    }

                    await ProcessMessageAsync(scope, eventArgs.RoutingKey, messageBody);

                    inboxRepository.Add(new InboxConsumedMessage
                    {
                        MessageId = eventId,
                        Handler = HandlerName,
                        ProcessedAt = DateTime.UtcNow
                    });

                    await unitOfWork.SaveChangesAsync();
                    await unitOfWork.CommitTransactionAsync();
                }, policyContext);

                stopwatch.Stop();
                // Было: "Successfully processed message. Latency: {LatencyMs}ms."
                _logger.LogInformation("Сообщение успешно обработано. Задержка: {LatencyMs} мс.",
                    stopwatch.ElapsedMilliseconds);
                await AcknowledgeMessageAsync(eventArgs.DeliveryTag);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // Было: "Failed to process message after all retries. Moving to DLX/NACKing. Latency: {LatencyMs}ms."
                _logger.LogError(ex,
                    "Не удалось обработать сообщение после всех попыток. Перемещение в карантин. Задержка: {LatencyMs} мс.",
                    stopwatch.ElapsedMilliseconds);

                await HandleDeadLetterAsync(null, $"Не удалось обработать после всех попыток: {ex.Message}", eventId,
                    messageBody);
                await NegativeAcknowledgeMessageAsync(eventArgs.DeliveryTag);
            }
        }
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

    private async Task NegativeAcknowledgeMessageAsync(ulong deliveryTag)
    {
        if (_channel is not null) await _channel.BasicNackAsync(deliveryTag, false, false);
    }

    protected abstract Task ProcessMessageAsync(IServiceScope scope, string routingKey, string messageBody);
}