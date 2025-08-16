using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Persistence.Repositories;

public class PostgresInboxRepository(ApplicationDbContext dbContext) : IInboxRepository
{
    public async Task<bool> IsHandledAsync(Guid messageId, string handlerName, CancellationToken cancellationToken)
    {
        return await dbContext.InboxConsumedMessages
            .AnyAsync(m => m.MessageId == messageId && m.Handler == handlerName, cancellationToken);
    }

    public void Add(InboxConsumedMessage message)
    {
        dbContext.InboxConsumedMessages.Add(message);
    }

    public void AddToDeadLetter(InboxDeadLetterMessage message)
    {
        dbContext.InboxDeadLetterMessages.Add(message);
    }
}