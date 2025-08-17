using AccountService.Shared.Domain;

namespace AccountService.Infrastructure.Persistence.Interfaces;


public interface IOutboxMessageRepository
{
    void Add(OutboxMessage message);
    
    void Update(OutboxMessage message); 
    Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken);
    
    Task<int> GetUnprocessedMessagesCountAsync(CancellationToken cancellationToken);
}