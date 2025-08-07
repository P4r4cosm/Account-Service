using AccountService.Features.Transactions;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Persistence.Repositories;

public class PostgresTransactionRepository(ApplicationDbContext dbContext): ITransactionRepository
{
    
    public async Task<PagedResult<Transaction>> GetPagedStatementAsync(Guid accountId, DateTime? startDate, DateTime? endDate, int pageNumber, int pageSize,
        CancellationToken cancellationToken)
    {
        // 1. Начинаем строить запрос НАПРЯМУЮ от таблицы Transactions в базе
        var query = dbContext.Transactions
            .Where(t => t.AccountId == accountId);

        // 2. Добавляем фильтры. Они будут транслированы в SQL WHERE
        if (startDate.HasValue)
            query = query.Where(t => t.Timestamp >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(t => t.Timestamp <= endDate.Value);

        // 3. Считаем общее количество записей В БАЗЕ ДАННЫХ
        var totalCount = await query.CountAsync(cancellationToken);
        
        // 4. Применяем сортировку и пагинацию. Это превратится в ORDER BY, OFFSET, LIMIT в SQL
        var pagedItems = await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedResult<Transaction>(pagedItems, totalCount, pageNumber, pageSize);
    }

    public async Task<Transaction?> GetByIdAndAccountIdAsync(Guid transactionId, Guid accountId, CancellationToken cancellationToken)
    {
        // Строим один запрос, который проверяет оба условия
        return await dbContext.Transactions
            .AsNoTracking() // Используем AsNoTracking, т.к. это read-only операция
            .FirstOrDefaultAsync(t => 
                    t.Id == transactionId && 
                    t.AccountId == accountId, 
                cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        await dbContext.Transactions.AddAsync(transaction, cancellationToken);
    }
}