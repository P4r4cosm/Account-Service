using System.Diagnostics.CodeAnalysis;
using AccountService.Features.Accounts;
using AccountService.Features.Accounts.GetAccounts;
using AccountService.Shared.Domain;

namespace AccountService.Infrastructure.Persistence.Interfaces;

[SuppressMessage("ReSharper",
    "UnusedParameter.Global")] //Resharper жалуется на неиспользование токена в реализациях, но он добавлен на будущее
public interface IAccountRepository
{
    Task<Features.Accounts.Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Features.Accounts.Account>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Features.Accounts.Account account, CancellationToken cancellationToken);
    Task DeleteAsync(Features.Accounts.Account account, CancellationToken cancellationToken);

    Task<bool> OwnerHasAccountsAsync(Guid ownerId, CancellationToken cancellationToken);

    Task<PagedResult<Account>> GetPagedAccountsAsync(
        GetAccountsQuery filters, 
        CancellationToken cancellationToken);

    Task UpdateAsync(Features.Accounts.Account account, CancellationToken cancellationToken);
}