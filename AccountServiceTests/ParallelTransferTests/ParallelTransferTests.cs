using System.Net;
using System.Net.Http.Json;
using AccountService.Features.Accounts;
using AccountService.Features.Transfers.CreateTransfer;
using AccountService.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountServiceTests.ParallelTransferTests;

public class ParallelTransferTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    
    [Fact]
    public async Task Transfers_ShouldMaintainTotalBalance_WhenRunInParallel()
    {
        // --- ARRANGE ---
        
        // Создаем 2 счета
        var fromAccountId = Guid.NewGuid();
        var toAccountId = Guid.NewGuid();
        const decimal initialFromBalance = 100_000m;
        const decimal initialToBalance = 100_000m;
        const decimal totalInitialBalance = initialFromBalance + initialToBalance;
        const decimal transferAmount = 100m;
        const int parallelRequests = 50;
        
        // Подготавливаем данные в БД
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Убедимся, что миграции применены
            await dbContext.Database.MigrateAsync();
            
            var fromAccount = new Account { 
                Id = fromAccountId, 
                Balance = initialFromBalance, 
                Currency = "RUB", 
                OpenedDate = DateTime.UtcNow 
            };
            var toAccount = new Account { 
                Id = toAccountId, 
                Balance = initialToBalance, 
                Currency = "RUB", 
                OpenedDate = DateTime.UtcNow 
            };
            
            await dbContext.Accounts.AddRangeAsync(fromAccount, toAccount);
            await dbContext.SaveChangesAsync();
        }

        // --- ACT ---
        var client = factory.CreateClient();
        var tasks = new List<Task<HttpResponseMessage>>();
        
       
        // Запускаем 50 параллельных запросов
        for (var i = 0; i < parallelRequests; i++)
        {
            var command = new CreateTransferCommand { 
                FromAccountId = fromAccountId, 
                ToAccountId = toAccountId, 
                Amount = transferAmount 
            };
            tasks.Add(client.PostAsJsonAsync("/api/transfers", command));
        }

        var responses = await Task.WhenAll(tasks);
        
        // --- ASSERT ---
        
        // Подсчитываем успешные и конфликтующие запросы
        var successfulTransfers = responses.Count(r => r.IsSuccessStatusCode);
        var conflictTransfers = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        var otherErrors = responses.Length - successfulTransfers - conflictTransfers;
        
        // Проверяем, что только одна транзакция завершилась успешно
        // Это критически важно для теста параллельных операций с оптимистичной блокировкой
        successfulTransfers.Should().Be(1, "только одна транзакция должна завершиться успешно из-за оптимистичной блокировки");
        
        // Проверяем, что остальные запросы вернули 409 Conflict
        conflictTransfers.Should().Be(parallelRequests - 1, "остальные запросы должны вернуть 409 Conflict");
        
        // Убедимся, что нет других ошибок
        otherErrors.Should().Be(0, "не должно быть других ошибок кроме 409 Conflict");
        
        // Проверяем балансы
        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var finalFromAccount = await dbContext.Accounts.FindAsync(fromAccountId);
            var finalToAccount = await dbContext.Accounts.FindAsync(toAccountId);

            var finalTotalBalance = finalFromAccount!.Balance + finalToAccount!.Balance;

            // Проверяем, что общая сумма балансов осталась неизменной
            finalTotalBalance.Should().Be(totalInitialBalance);
            
            // Проверяем, что балансы изменились ровно на одну транзакцию
            finalFromAccount.Balance.Should().Be(initialFromBalance - transferAmount);
            finalToAccount.Balance.Should().Be(initialToBalance + transferAmount);
        }
    }
}