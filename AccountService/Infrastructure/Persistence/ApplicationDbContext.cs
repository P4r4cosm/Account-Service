using System.Data;
using AccountService.Features.Transactions;
using AccountService.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using AccountService.Features.Accounts;
using Microsoft.EntityFrameworkCore.Storage;

namespace AccountService.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    private IDbContextTransaction? _currentTransaction; // Поле для хранения активной транзакции


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Конфигурация для таблицы Accounts
        modelBuilder.Entity<Account>(entity =>
        {
            // 1. Hash-индекс по колонке ownerId
            entity.HasIndex(a => a.OwnerId).HasMethod("hash");
        });

        // Конфигурация для таблицы Transactions
        modelBuilder.Entity<Transaction>(entity =>
        {
            // 2. Составной индекс (accountId, "date")
            entity.HasIndex(t => new { t.AccountId, t.Timestamp });

            // 3. GiST-индекс по колонке "date" для выборок по диапазону
            // Для создания двух индексов на одно поле, нужно дать им имена
            entity.HasIndex(t => t.Timestamp, "IX_Transactions_Date_Default"); // Обычный B-Tree по умолчанию
            entity.HasIndex(t => t.Timestamp, "IX_Transactions_Date_Gist")
                .HasMethod("gist")
                .HasDatabaseName("IX_Transactions_Date_Gist");
        });
    }

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel,
        CancellationToken cancellationToken = default)
    {
        // Начинаем транзакцию, только если она еще не начата
        if (_currentTransaction is not null)
        {
            return;
        }

        // Делегируем вызов EF Core и сохраняем объект транзакции в наше поле
        _currentTransaction = await Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем, что транзакция была начата
            if (_currentTransaction is null)
            {
                throw new InvalidOperationException("Транзакция не была начата.");
            }

            // Фиксируем изменения
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            // В любом случае освобождаем ресурсы и сбрасываем состояние
            if (_currentTransaction is not null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Проверяем, что транзакция была начата
            if (_currentTransaction is null)
            {
                throw new InvalidOperationException("Транзакция не была начата.");
            }

            // Откатываем изменения
            await _currentTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            // В любом случае освобождаем ресурсы и сбрасываем состояние
            if (_currentTransaction is not null)
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }
    }
}