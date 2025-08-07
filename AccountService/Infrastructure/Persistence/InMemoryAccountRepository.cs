using System.Collections.Concurrent;
using AccountService.Features.Accounts;

namespace AccountService.Infrastructure.Persistence;

public class InMemoryAccountRepository: IAccountRepository
{
    // Ключ - Guid (Id счёта), Значение - сам объект Account.
    // Это потокобезопасная коллекция, созданная специально для многопоточных сценариев, как в Web API.
    // Она также обеспечивает быстрый поиск по ключу (O(1) в среднем) в отличие от перебора списка (O(n)).
    private static readonly ConcurrentDictionary<Guid, Account> Accounts = new();

    public Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Accounts.TryGetValue(id, out var account);
        return Task.FromResult(account);
    }

    public Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Account>>(Accounts.Values);
    }

    public Task AddAsync(Account account, CancellationToken cancellationToken = default)
    {
        Accounts.TryAdd(account.Id, account);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Account account, CancellationToken cancellationToken = default)
    {
        Accounts.TryRemove(account.Id, out _); // Второй параметр (out) нам не нужен, поэтому используем _
        return Task.CompletedTask;
    }

    public Task<bool> OwnerHasAccountsAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var hasAccounts = Accounts.Values.Any(x => x.OwnerId == ownerId);
        return Task.FromResult(hasAccounts);
    }

    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        Accounts[account.Id] = account;
        return Task.CompletedTask;
    }
}