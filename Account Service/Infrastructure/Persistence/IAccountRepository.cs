using BankAccounts.Features.Accounts;

namespace BankAccounts.Infrastructure.Persistence;

public interface IAccountRepository
{
    Task<Account?>  GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<List<Account>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
}