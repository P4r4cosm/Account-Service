using System.Diagnostics.CodeAnalysis;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.GetAccounts;
using AccountService.Shared.Domain;

namespace AccountService.Infrastructure.Persistence.Interfaces;

[SuppressMessage("ReSharper",
    "UnusedParameter.Global")] //Resharper жалуется на неиспользование токена в реализациях, но он добавлен на будущее
public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    
    // Метод использующийся в InMemoryAccountRepository
    // Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task DeleteAsync(Account account, CancellationToken cancellationToken);

    Task<bool> OwnerHasAccountsAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<PagedResult<Account>> GetPagedAccountsAsync(
        GetAccountsQuery filters, 
        CancellationToken cancellationToken);

    Task UpdateAsync(Account account, CancellationToken cancellationToken);
    
}