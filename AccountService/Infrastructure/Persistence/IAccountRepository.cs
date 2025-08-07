using System.Diagnostics.CodeAnalysis;
using AccountService.Features.Accounts;

namespace AccountService.Infrastructure.Persistence;

[SuppressMessage("ReSharper", "UnusedParameter.Global")] //Resharper жалуется на неиспользование токена в реализациях, но он добавлен на будущее
public interface IAccountRepository
{
    Task<Account?>  GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task DeleteAsync(Account account, CancellationToken cancellationToken);

    Task<bool> OwnerHasAccountsAsync(Guid ownerId, CancellationToken cancellationToken);
    
    Task UpdateAsync(Account account, CancellationToken cancellationToken);
    
}