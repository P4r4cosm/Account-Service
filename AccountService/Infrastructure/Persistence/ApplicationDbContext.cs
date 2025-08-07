using AccountService.Features.Accounts;
using AccountService.Features.Transactions;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

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
}