using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Events;
using RabbitMQ.Client;

namespace AccountService.Infrastructure.Persistence.MessageBroker;

public class AntifraudConsumer(
    IConnection connection,
    IServiceProvider serviceProvider,
    ILogger<AntifraudConsumer> logger)
    : RabbitMqConsumerBase(connection, serviceProvider, logger)
{
    // Определяем константы, специфичные для этого консьюмера
    protected override string QueueName => "account.antifraud";
    protected override string HandlerName => nameof(AntifraudConsumer);

    protected override async Task ProcessMessageAsync(IServiceScope scope, string routingKey, string messageBody)
    {
        var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();

        switch (routingKey)
        {
            case "client.blocked":
                var blockedEvent = JsonSerializer.Deserialize<EventEnvelope<ClientBlockedEvent>>(messageBody);
                if (blockedEvent is not null)
                {
                    logger.LogInformation("Processing ClientBlocked event for ClientId: {ClientId}",
                        blockedEvent.Payload.ClientId);
                    var frozenCount =
                        await accountRepository.FreezeAccountsByOwnerAsync(blockedEvent.Payload.ClientId, CancellationToken.None);
                    logger.LogInformation("Froze {Count} accounts for ClientId: {ClientId}", frozenCount,
                        blockedEvent.Payload.ClientId);
                }

                break;

            case "client.unblocked":
                var unblockedEvent = JsonSerializer.Deserialize<EventEnvelope<ClientUnblockedEvent>>(messageBody);
                if (unblockedEvent is not null)
                {
                    logger.LogInformation("Processing ClientUnblocked event for ClientId: {ClientId}",
                        unblockedEvent.Payload.ClientId);
                    var unfrozenCount =
                        await accountRepository.UnfreezeAccountsByOwnerAsync(unblockedEvent.Payload.ClientId, CancellationToken.None);
                    logger.LogInformation("Unfroze {Count} accounts for ClientId: {ClientId}", unfrozenCount,
                        unblockedEvent.Payload.ClientId);
                }

                break;
            default:
                logger.LogWarning("Received message with unhandled routing key '{RoutingKey}' in {HandlerName}.",
                    routingKey, HandlerName);
                break;
        }
    }
}