using AccountService.Features.Accounts;
using AccountService.Features.Accounts.GetAccounts;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AccountService.Infrastructure.Persistence.Repositories;

public class PostgresAccountRepository(ApplicationDbContext dbContext) : IAccountRepository
{
    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        // Используем FindAsync для оптимального поиска по первичному ключу
        return await dbContext.Accounts.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<Account>> GetAllAsync(CancellationToken cancellationToken)
    {
        // AsNoTracking() полезен для операций "только для чтения", так как EF не будет отслеживать изменения,
        // что экономит память и процессорное время. Для простого списка это идеально.
        return await dbContext.Accounts.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        await dbContext.Accounts.AddAsync(account, cancellationToken);
    }


    /// <summary>
    /// Устанавливает CloseDate значение, равное времени в которое выполнялась операция 
    /// </summary>
    /// <param name="account"></param>
    /// <param name="cancellationToken"></param>
    public Task DeleteAsync(Account account, CancellationToken cancellationToken)
    {
        account.CloseDate = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public async Task<bool> OwnerHasAccountsAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await dbContext.Accounts.AnyAsync(a => a.OwnerId == ownerId, cancellationToken);
    }

    public Task UpdateAsync(Account account, CancellationToken cancellationToken)
    {
        dbContext.Accounts.Update(account);
        return Task.CompletedTask;
    }

    public async Task AccrueInterest(Guid accountId,CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlAsync(
            $"CALL accrue_interest({accountId})",
            cancellationToken);
    }

    public async Task<List<Guid>> GetAccountIdsForAccrueInterestAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Accounts
            .Where(a => a.AccountType == AccountType.Deposit && a.CloseDate == null && a.Balance > 0)
            .Select(a => a.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Account>> GetPagedAccountsAsync(
        GetAccountsQuery filters,
        CancellationToken cancellationToken)
    {
        // 1. Начинаем строить запрос от таблицы в БД.
        // Используем AsNoTracking, так как это read-only операция.
        var query = dbContext.Accounts.AsNoTracking();

        // 2. Динамически добавляем условия WHERE, если фильтры были переданы.
        // Это все будет транслировано в SQL.
        if (filters.OwnerId.HasValue)
        {
            query = query.Where(a => a.OwnerId == filters.OwnerId.Value);
        }

        if (!string.IsNullOrEmpty(filters.AccountType))
        {
            if (Enum.TryParse<AccountType>(filters.AccountType, true, out var accountTypeEnum))
            {
                query = query.Where(a => a.AccountType == accountTypeEnum);
            }
        }

        if (!string.IsNullOrEmpty(filters.Currency))
        {
            query = query.Where(a => a.Currency == filters.Currency);
        }

        if (filters.BalanceGte.HasValue)
        {
            query = query.Where(a => a.Balance >= filters.BalanceGte.Value);
        }

        if (filters.BalanceLte.HasValue)
        {
            query = query.Where(a => a.Balance <= filters.BalanceLte.Value);
        }

        if (filters.OpeningDateFrom.HasValue)
        {
            filters.OpeningDateFrom = DateTime.SpecifyKind((DateTime)filters.OpeningDateFrom, DateTimeKind.Utc);
            query = query.Where(a => a.OpenedDate >= filters.OpeningDateFrom.Value);
        }

        if (filters.OpeningDateTo.HasValue)
        {
            // Добавляем один день и ищем "меньше", чтобы включить весь день до 23:59:59
            var dateTo = filters.OpeningDateTo.Value.Date.AddDays(1);
            dateTo = DateTime.SpecifyKind(dateTo, DateTimeKind.Utc);
            query = query.Where(a => a.OpenedDate < dateTo);
        }

        // 3. Сначала считаем общее количество записей ПОСЛЕ фильтрации В БАЗЕ.
        var totalCount = await query.CountAsync(cancellationToken);

        // 4. Затем применяем пагинацию.
        var pagedItems = await query
            .Skip((filters.PageNumber - 1) * filters.PageSize)
            .Take(filters.PageSize)
            .ToListAsync(cancellationToken);

        // 5. Возвращаем результат.
        return new PagedResult<Account>(pagedItems, totalCount, filters.PageNumber, filters.PageSize);
    }
}