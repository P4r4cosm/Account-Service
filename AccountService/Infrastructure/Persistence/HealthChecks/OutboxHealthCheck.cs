using AccountService.Infrastructure.Persistence.Interfaces;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AccountService.Infrastructure.Persistence.HealthChecks;

/// <summary>
/// Проверяет состояние таблицы Outbox.
/// </summary>
public class OutboxHealthCheck(IOutboxMessageRepository outboxRepository) : IHealthCheck
{
    private const int LagThreshold = 100; // Порог для статуса "Degraded"

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Предполагается, что у вас есть или вы добавите такой метод в репозиторий.
            // Он эффективнее, чем получать все сообщения и считать их в памяти.
            var unprocessedCount = await outboxRepository.GetUnprocessedMessagesCountAsync(cancellationToken);

            if (unprocessedCount < LagThreshold)
            {
                // Если сообщений мало, все в порядке.
                return HealthCheckResult.Healthy(
                    $"Outbox в порядке. Необработанных сообщений: {unprocessedCount}.");
            }
            
            // Если сообщений много, это не критическая ошибка, а предупреждение.
            return HealthCheckResult.Degraded( 
                $"Отставание Outbox. Необработанных сообщений: {unprocessedCount} (порог: {LagThreshold}).");
        }
        catch (Exception ex)
        {
            // Если мы даже не можем выполнить запрос к БД, это критическая ошибка.
            return HealthCheckResult.Unhealthy(
                "Не удалось проверить состояние Outbox.",
                exception: ex);
        }
    }
}