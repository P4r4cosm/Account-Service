using AccountService.Features.Accounts;

namespace AccountService.Infrastructure.Persistence;

public interface IAccountRepository
{
    Task<Account?>  GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task DeleteAsync(Account account, CancellationToken cancellationToken);

    Task<bool> OwnerHasAccountsAsync(Guid ownerId, CancellationToken cancellationToken);
    
    Task UpdateAsync(Account account, CancellationToken cancellationToken);
    
}