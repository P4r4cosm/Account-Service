using BankAccounts.Features.Accounts;

namespace BankAccounts.Infrastructure.Persistence;

public class InMemoryAccountRepository: IAccountRepository
{
    private static readonly List<Account> _accounts= new();
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = _accounts.FirstOrDefault(x => x.Id == id);
        return Task.FromResult(account);
    }

    public Task<List<Account>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_accounts);
    }

    public Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        _accounts.Add(account);
        return Task.CompletedTask;
    }
}