using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;

namespace AccountService.Infrastructure.Persistence.Repositories;

public class PostgresOutboxMessageRepository(ApplicationDbContext context) : IOutboxMessageRepository
{
    public void Add(OutboxMessage message)
    {
        context.OutboxMessages.Add(message);
    }
}