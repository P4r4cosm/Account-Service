using AccountService.Features.Transactions;
using AccountService.Shared.Domain;

namespace AccountService.Infrastructure.Persistence.Interfaces;

public interface ITransactionRepository
{
    Task<PagedResult<Transaction>> GetPagedStatementAsync(
        Guid accountId, 
        DateTime? startDate, 
        DateTime? endDate, 
        int pageNumber, 
        int pageSize,
        CancellationToken cancellationToken);
    
    Task<Transaction?> GetByIdAndAccountIdAsync(Guid transactionId, Guid accountId, CancellationToken cancellationToken);
    
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
}