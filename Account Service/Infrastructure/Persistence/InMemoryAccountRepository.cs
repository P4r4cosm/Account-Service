using AccountService.Features.Accounts;

namespace AccountService.Infrastructure.Persistence;

public class InMemoryAccountRepository: IAccountRepository
{
    private static readonly List<Account> _accounts= new();
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken=default)
    {
        var account = _accounts.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(account);
    }

    public Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken=default)
    {
        return Task.FromResult<IEnumerable<Account>>(_accounts);
    }

    public Task AddAsync(Account account, CancellationToken cancellationToken=default)
    {
        _accounts.Add(account);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Account account, CancellationToken cancellationToken=default)
    {
        _accounts.Remove(account);
        return Task.CompletedTask;
    }

    public Task<bool> OwnerHasAccountsAsync(Guid ownerId, CancellationToken cancellationToken=default)
    {
        var hasAccounts= _accounts.Any(x => x.OwnerId == ownerId);
        return Task.FromResult(hasAccounts);
    }
    

    public Task UpdateAsync(Account account, CancellationToken cancellationToken=default)
    {
        var index = _accounts.FindIndex(a => a.Id == account.Id);
        
        if (index != -1)
        {
            _accounts[index] = account;
        }
        return Task.CompletedTask;
    }
}