using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Events;
using RabbitMQ.Client;

namespace AccountService.Infrastructure.Persistence.MessageBroker;

/// <summary>
/// Потребитель сообщений для обработки событий блокировки/разблокировки клиентов.
/// </summary>
public class AntifraudConsumer(
    IConnection connection,
    IServiceProvider serviceProvider,
    ILogger<AntifraudConsumer> logger)
    : RabbitMqConsumerBase(connection, serviceProvider, logger)
{
    // Определяем константы, специфичные для этого потребителя
    protected override string QueueName => "account.antifraud";
    protected override string HandlerName => nameof(AntifraudConsumer);

    protected override async Task ProcessMessageAsync(IServiceScope scope, string routingKey, string messageBody)
    {
        // Получаем зависимости из текущего scope, это лучшая практика
        var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
        var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<AntifraudConsumer>>();

        switch (routingKey)
        {
            case "client.blocked":
                var blockedEvent = JsonSerializer.Deserialize<EventEnvelope<ClientBlockedEvent>>(messageBody);
                if (blockedEvent is not null)
                {
                    scopedLogger.LogInformation("Обработка события ClientBlocked для клиента с ID: {ClientId}",
                        blockedEvent.Payload.ClientId);

                    var frozenCount =
                        await accountRepository.FreezeAccountsByOwnerAsync(blockedEvent.Payload.ClientId,
                            CancellationToken.None);


                    scopedLogger.LogInformation("Заморожено {Count} счетов для клиента с ID: {ClientId}", frozenCount,
                        blockedEvent.Payload.ClientId);
                }

                break;

            case "client.unblocked":
                var unblockedEvent = JsonSerializer.Deserialize<EventEnvelope<ClientUnblockedEvent>>(messageBody);
                if (unblockedEvent is not null)
                {
                    scopedLogger.LogInformation("Обработка события ClientUnblocked для клиента с ID: {ClientId}",
                        unblockedEvent.Payload.ClientId);

                    var unfrozenCount =
                        await accountRepository.UnfreezeAccountsByOwnerAsync(unblockedEvent.Payload.ClientId,
                            CancellationToken.None);


                    scopedLogger.LogInformation("Разморожено {Count} счетов для клиента с ID: {ClientId}",
                        unfrozenCount,
                        unblockedEvent.Payload.ClientId);
                }

                break;

            default:

                scopedLogger.LogWarning(
                    "Получено сообщение с необрабатываемым ключом маршрутизации '{RoutingKey}' в обработчике {HandlerName}.",
                    routingKey, HandlerName);
                break;
        }
    }
}