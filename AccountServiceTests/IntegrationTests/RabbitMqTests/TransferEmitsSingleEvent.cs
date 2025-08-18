using System.Text;
using System.Text.Json;
using AccountService;
using AccountService.Features.Transfers.CreateTransfer;
using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace AccountServiceTests.IntegrationTests.RabbitMqTests;

[Collection("Sequential")]
public class TransferEmitsSingleEvent(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task TransferEmitsSingleEvent_ForFiftyTransfers()
    {
        // ARRANGE - Шаг 1: Создаем счета и пополняем исходный
        output.WriteLine("Шаг 1: Подготовка счетов...");
        var ownerId = Guid.NewGuid();
        var sourceAccountId = await CreateAccountDirectly(ownerId, initialBalance: 1000);
        var destinationAccountId = await CreateAccountDirectly(ownerId);

        await using var initialScope = factory.Services.CreateAsyncScope();
        var dbContext = initialScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Считаем только события, относящиеся к переводам
        var eventsBefore = await dbContext.OutboxMessages
            .CountAsync(m => m.Type == nameof(TransferCompletedEvent));
        eventsBefore.Should().Be(0);

        // ACT - Шаг 2: Выполняем 50 переводов
        output.WriteLine("Шаг 2: Выполнение 50 переводов...");
        const int transfersCount = 50;
        const decimal transferAmount = 10m;

        for (var i = 0; i < transfersCount; i++)
        {
            var transferCommand = new CreateTransferCommand
            {
                CommandId = Guid.NewGuid(),
                FromAccountId = sourceAccountId,
                ToAccountId = destinationAccountId,
                Amount = transferAmount,
                Description = "Test Transfer" // Валюта должна соответствовать валюте счетов
            };
            
            var content = new StringContent(JsonSerializer.Serialize(transferCommand), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/transfers", content); // Укажите правильный URL вашего эндпоинта
            
            // Проверяем каждую операцию на успех, чтобы тест упал сразу при проблеме
            response.EnsureSuccessStatusCode();
        }

        // ASSERT - Шаг 3: Проверяем результат
        output.WriteLine("Шаг 3: Проверка количества событий и балансов...");

        // 3.1. Проверяем количество сгенерированных событий
        var eventsAfter = await dbContext.OutboxMessages
            .CountAsync(m => m.Type == nameof(TransferCompletedEvent));
        
        (eventsAfter - eventsBefore).Should().Be(transfersCount, $"должно быть создано ровно {transfersCount} событий TransferCompleted");

        // 3.2. (Опционально, но рекомендуется) Проверяем конечные балансы
        var sourceAccount = await dbContext.Accounts.FindAsync(sourceAccountId);
        var destinationAccount = await dbContext.Accounts.FindAsync(destinationAccountId);

        sourceAccount.Should().NotBeNull();
        destinationAccount.Should().NotBeNull();
        
        sourceAccount.Balance.Should().Be(1000 - transfersCount * transferAmount, "баланс исходного счета должен уменьшиться на общую сумму переводов");
        destinationAccount.Balance.Should().Be(0 + transfersCount * transferAmount, "баланс целевого счета должен увеличиться на общую сумму переводов");
        
        output.WriteLine("Тест успешно завершен.");
    }
    
    // Вспомогательный метод для создания и опционального пополнения счета
    private async Task<Guid> CreateAccountDirectly(Guid ownerId, decimal initialBalance = 0)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var createCommand = new AccountService.Features.Accounts.CreateAccount.CreateAccountCommand
        {
            OwnerId = ownerId, AccountType = "Checking", Currency = "RUB"
        };
        var createResult = await mediator.Send(createCommand);
        var accountId = createResult.Value!.Id;

        if (initialBalance <= 0) return accountId;
        var account = await dbContext.Accounts.FindAsync(accountId);
        account!.Balance = initialBalance;
        await dbContext.SaveChangesAsync();

        return accountId;
    }
}