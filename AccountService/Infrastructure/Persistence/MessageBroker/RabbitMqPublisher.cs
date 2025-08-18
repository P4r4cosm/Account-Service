using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Behaviors;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using AccountService.Shared.Options;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace AccountService.Infrastructure.Persistence.MessageBroker;

public class RabbitMqMessagePublisher : IMessagePublisher
{
    private readonly IConnection _connection;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqMessagePublisher> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly IEventRoutingKeyMapper _routingKeyMapper;
    

    public RabbitMqMessagePublisher(
        IConnection connection,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqMessagePublisher> logger,
        IEventRoutingKeyMapper routingKeyMapper)
    {
        _connection = connection;
        _options = options.Value;
        _logger = logger;
        _routingKeyMapper = routingKeyMapper;


        _retryPolicy = Policy
            // Указываем, какие исключения свидетельствуют о временном сбое
            .Handle<BrokerUnreachableException>() // RabbitMQ недоступен
            .Or<SocketException>() // Проблемы с сетью на низком уровне
            .Or<AlreadyClosedException>() // Соединение или канал были неожиданно закрыты
            // Стратегия ожидания: 3 попытки с растущей задержкой (2с, 4с, 6с)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(retryAttempt * 2),
                onRetry: (exception, timeSpan, retryCount, _) =>
                {
                    // Логируем каждую неудачную попытку. Это очень важно для диагностики!
                    _logger.LogWarning(exception,
                        "Сбой при публикации в RabbitMQ. Попытка {RetryCount} через {TimeSpan}.",
                        retryCount, timeSpan);
                });
    }


    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var routingKey = _routingKeyMapper.GetRoutingKey(message.Type);

        try
        {
            // Если внутри лямбды произойдет одно из отслеживаемых исключений, Polly выполнит повтор.
            await _retryPolicy.ExecuteAsync(async ct =>
            {
                // Если соединение разорвано, нет смысла даже пытаться создать канал.
                if (!_connection.IsOpen)
                {
                    _logger.LogError("Соединение с RabbitMQ закрыто. Публикация невозможна.");
                    // Выбрасываем исключение, чтобы Polly и OutboxProcessor знали о сбое.
                    throw new BrokerUnreachableException(new Exception("Соединение с RabbitMQ не открыто."));
                }

                // Создаем канал с включенными подтверждениями
                var channelOpts = new CreateChannelOptions(true, true);
                await using var channel = await _connection.CreateChannelAsync(channelOpts, cancellationToken: ct);


                var eventEnvelope = JsonSerializer.Deserialize<EventEnvelope<object>>(message.Payload);
                if (eventEnvelope is null)
                {
                    _logger.LogError(
                        "Критическая ошибка: не удалось десериализовать сообщение из Outbox с Id: {MessageId}",
                        message.Id);

                    return;
                }


                var properties = new BasicProperties
                {
                    Persistent = true,
                    MessageId = message.Id.ToString(),
                    CorrelationId = message.CorrelationId.ToString(),
                    ContentType = "application/json",
                    Headers = new Dictionary<string, object>
                    {
                        { "X-Correlation-Id", message.CorrelationId.ToString() },
                        { "X-Causation-Id", eventEnvelope.Meta.CausationId.ToString() }
                    }!
                };

                var body = Encoding.UTF8.GetBytes(message.Payload);

                // Асинхронная публикация
                await channel.BasicPublishAsync(
                    exchange: _options.ExchangeName,
                    routingKey: routingKey,
                    mandatory: true,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct);
            }, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation(
                "Сообщение {MessageId} успешно опубликовано. Ключ: {RoutingKey}, CorrelationId: {CorrelationId}, Затрачено: {LatencyMs}ms.",
                message.Id, routingKey, message.CorrelationId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Не удалось опубликовать сообщение {MessageId} после всех попыток. Ключ: {RoutingKey}, CorrelationId: {CorrelationId}, Затрачено: {LatencyMs}ms.",
                message.Id, routingKey, message.CorrelationId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}