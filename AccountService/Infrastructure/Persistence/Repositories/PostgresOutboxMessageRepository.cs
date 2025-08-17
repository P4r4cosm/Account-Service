using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Persistence.Repositories;

public class PostgresOutboxMessageRepository(ApplicationDbContext context) : IOutboxMessageRepository
{
    public void Add(OutboxMessage message)
    {
        context.OutboxMessages.Add(message);
    }

    public void Update(OutboxMessage message)
    {
        context.OutboxMessages.Update(message);
    }

    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken)
    {
        return await context.OutboxMessages
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(20) // Ограничиваем выборку для производительности
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnprocessedMessagesCountAsync(CancellationToken cancellationToken)
    {
        return await context.OutboxMessages
            .CountAsync(m => m.ProcessedAt == null, cancellationToken);
    }
}