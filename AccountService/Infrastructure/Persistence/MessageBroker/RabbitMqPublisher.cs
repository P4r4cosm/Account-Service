using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using AccountService.Shared.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AccountService.Infrastructure.Persistence.MessageBroker;

public class RabbitMqMessagePublisher(
    IConnection connection,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqMessagePublisher> logger) : IMessagePublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            var eventEnvelope = JsonSerializer.Deserialize<EventEnvelope<object>>(message.Payload);
            if (eventEnvelope is null)
            {
                logger.LogError("Не удалось десериализовать сообщение из Outbox с Id: {MessageId}", message.Id);
                return;
            }

            var properties = new BasicProperties
            {
                Persistent = true,
                MessageId = message.Id.ToString(),
                CorrelationId = message.CorrelationId.ToString(),
                ContentType = "application/json",
                // Добавляем заголовки
                Headers = new Dictionary<string, object>
                {
                    { "X-Correlation-Id", message.CorrelationId.ToString() },
                    { "X-Causation-Id", eventEnvelope.Meta.CausationId.ToString() }
                }!
            };


            var routingKey = GetRoutingKey(message.Type);
            var body = Encoding.UTF8.GetBytes(message.Payload);

            await channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
            
            stopwatch.Stop();

            logger.LogInformation(
                "Сообщение успешно опубликовано. Ключ: {RoutingKey}, Задержка: {LatencyMs}ms.",
                routingKey, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "Сбой при публикации в RabbitMQ. Задержка: {LatencyMs}ms.", stopwatch.ElapsedMilliseconds);
            throw; 
        }
    }

    private static string GetRoutingKey(string eventType)
    {
        return eventType switch
        {
            nameof(AccountOpenedEvent) => "account.opened",
            nameof(MoneyCreditedEvent) => "money.credited",
            nameof(MoneyDebitedEvent) => "money.debited",
            // для AccountOwnerChangedEvent и AccountInterestRateChangedEvent пришлось использовать неправильное написание,
            // чтобы попасть в account.crm (routing key: account.*)
            nameof(AccountOwnerChangedEvent) => "account.ownerChanged", 
            nameof(AccountInterestRateChangedEvent) => "account.rateChanged",
            nameof(AccountReopenedEvent) => "account.reopened", 
            nameof(TransferCompletedEvent) => "money.transfer.completed",
            nameof(InterestAccruedEvent) => "money.interest.accrued",
            nameof(AccountClosedEvent) => "account.closed",
            _ => "unknown"
        };
    }
}