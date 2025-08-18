using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using AccountService;
using AccountService.Features.Accounts.CreateAccount;
using AccountService.Features.Transactions.RegisterTransaction;
using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Events;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Xunit.Abstractions;

namespace AccountServiceTests.IntegrationTests.RabbitMqTests;

[Collection("Sequential")]
public class ClientBlockedPreventsDebit(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ClientBlocked_PreventsDebit_And_NoEventIsPublished()
    {
        // ARRANGE - Шаг 1: Создаем клиента, счет и пополняем его
        output.WriteLine("Шаг 1: Создание и пополнение счета...");
        var ownerId = Guid.NewGuid();
        
        // создаём аккаунт
        var accountId = await CreateAccountDirectly(ownerId);

        // Пополняем счет, чтобы было что списывать
        var creditCommand = new RegisterTransactionCommand
        {
            Amount = 1000,
            Type = "Credit",
            Description = "Initial deposit"
        };
        var creditContent =
            new StringContent(JsonSerializer.Serialize(creditCommand), Encoding.UTF8, "application/json");
        var creditResponse = await _client.PostAsync($"/api/accounts/{accountId}/transactions", creditContent);
        creditResponse.EnsureSuccessStatusCode();

        await using var initialCheckScope = factory.Services.CreateAsyncScope();
        var dbContext = initialCheckScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var account = await dbContext.Accounts.FindAsync(accountId);
        account.Should().NotBeNull();
        account.IsFrozen.Should().BeFalse("счет не должен быть заморожен изначально");
        account.Balance.Should().Be(1000);

        var messagesBeforeBlock = await dbContext.OutboxMessages.Where(o=>o.Type!=nameof(AccountOpenedEvent)).CountAsync();
        messagesBeforeBlock.Should().Be(1, "должно быть одно событие MoneyCredited после пополнения");

        // ACT - Шаг 2: Публикуем событие ClientBlocked
        output.WriteLine("Шаг 2: Публикация события ClientBlocked...");
        await PublishClientBlockedEvent(ownerId);

        // ASSERT - Шаг 3: Ждем и проверяем, что счет заморожен
        output.WriteLine("Шаг 3: Ожидание обработки события и проверка заморозки счета...");
        
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(10)) // Таймаут 10 секунд
        {
            await dbContext.Entry(account).ReloadAsync();
            if (account.IsFrozen) break;
            await Task.Delay(200);
        }

        account.IsFrozen.Should().BeTrue("потребитель должен был обработать событие и заморозить счет");

        // ACT - Шаг 4: Пытаемся списать деньги
        output.WriteLine("Шаг 4: Попытка списания средств с замороженного счета...");
        var debitCommand = new RegisterTransactionCommand
        {
            Amount = 500,
            Type = "Debit",
            Description = "Attempted withdrawal"
        };
        var debitContent = new StringContent(JsonSerializer.Serialize(debitCommand), Encoding.UTF8, "application/json");
        var debitResponse = await _client.PostAsync($"/api/accounts/{accountId}/transactions", debitContent);

        // ASSERT - Шаг 5: Проверяем результат
        output.WriteLine("Шаг 5: Проверка ответа API и состояния системы...");

        // 5.1. API вернул 409 Conflict
        debitResponse.StatusCode.Should().Be(HttpStatusCode.Conflict,
            "API должен вернуть Conflict при попытке списания с замороженного счета");

        // 5.2. Баланс счета не изменился
        await dbContext.Entry(account).ReloadAsync();
        account.Balance.Should().Be(1000, "баланс не должен был измениться");

        // 5.3. Новое событие MoneyDebited НЕ было опубликовано
        var messagesAfterDebitAttempt = await dbContext.OutboxMessages.Where(o=>o.Type!=nameof(AccountOpenedEvent)).CountAsync();
        messagesAfterDebitAttempt.Should()
            .Be(messagesBeforeBlock, "количество сообщений в outbox не должно было увеличиться");

        output.WriteLine("Тест успешно завершен.");
    }

    // Вспомогательный метод для прямого создания счета, если нет API
    private async Task<Guid> CreateAccountDirectly(Guid ownerId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
        var command = new CreateAccountCommand
        {
            OwnerId = ownerId,
            AccountType = "Checking",
            Currency = "RUB"
        };
        var result = await mediator.Send(command);
        return result.Value!.Id;
    }

    // Вспомогательный метод для публикации сообщения в RabbitMQ
    private async Task PublishClientBlockedEvent(Guid clientId)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = factory.RabbitMqContainer.Hostname,
            Port = factory.RabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "guest",
            Password = "guest"
        };

        await using var connection = await connectionFactory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        var blockedEvent = new ClientBlockedEvent { ClientId = clientId };
        var envelope = new EventEnvelope<ClientBlockedEvent>(blockedEvent, Guid.NewGuid(), Guid.NewGuid());

        var messageBody = JsonSerializer.Serialize(envelope);
        var bodyBytes = Encoding.UTF8.GetBytes(messageBody);

        var properties = new BasicProperties
        {
            MessageId = envelope.EventId.ToString(),
            CorrelationId = envelope.Meta.CorrelationId.ToString(),
            ContentType = "application/json",
            Persistent = true
        };

        await channel.BasicPublishAsync(
            exchange: "account.events", // Убедитесь, что имя exchange верное
            routingKey: "client.blocked",
            basicProperties: properties,
            mandatory: true,
            body: bodyBytes,
            cancellationToken: CancellationToken.None
        );
    }
}