using System.Text;
using System.Text.Json;
using AccountService;
using AccountService.Features.Accounts.CreateAccount;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Persistence.HangfireServices;
using AccountService.Shared.Events;
using FluentAssertions;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Xunit.Abstractions;

namespace AccountServiceTests.IntegrationTests.RabbitMqTests;

[Collection("Sequential")] // Гарантирует, что тесты, манипулирующие контейнерами, не будут конфликтовать
public class OutboxPublishesAfterFailure(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
    : IClassFixture<CustomWebApplicationFactory<Program>>
{
    [Fact]
    public async Task Outbox_ShouldReliablyPublishMessage_AfterBrokerFailure()
    {
        // ARRANGE - Шаг 1: Создаем событие в Outbox (без изменений)
        output.WriteLine("Шаг 1: Создание счета и запись события в Outbox...");
        var ownerId = Guid.NewGuid();
        var createAccountCommand = new CreateAccountCommand
            { OwnerId = ownerId, AccountType = "Checking", Currency = "RUB" };
        factory.CreateClient();

        Guid eventId;
        await using (var initialScope = factory.Services.CreateAsyncScope())
        {
            var mediator = initialScope.ServiceProvider.GetRequiredService<IMediator>();
            var dbContext = initialScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.MigrateAsync();
            
            var createResult = await mediator.Send(createAccountCommand);
            createResult.IsSuccess.Should().BeTrue();
            var outboxMessage = await dbContext.OutboxMessages.AsNoTracking().AsNoTracking().SingleAsync();
            outboxMessage.ProcessedAt.Should().BeNull();
            // Десериализуем, чтобы получить eventId для последующей фильтрации
            var deserializedEvent =
                JsonSerializer.Deserialize<EventEnvelope<AccountOpenedEvent>>(outboxMessage.Payload);
            eventId = deserializedEvent!.EventId;
        }

        output.WriteLine("Событие успешно создано в Outbox. Ищем EventId: " + eventId);

        // ACT & ASSERT (Часть 1) - Шаг 2: Имитируем сбой RabbitMQ
        output.WriteLine("\nШаг 2: Остановка RabbitMQ и попытка обработки Outbox...");
        await factory.RabbitMqContainer.StopAsync();
        output.WriteLine("Контейнер RabbitMQ остановлен.");
        await using (var failedAttemptScope = factory.Services.CreateAsyncScope())
        {
            var outboxProcessor = failedAttemptScope.ServiceProvider.GetRequiredService<OutboxProcessorJob>();
            try
            {
                await outboxProcessor.ProcessOutboxMessagesAsync(new JobCancellationToken(false));
            }
            catch (Exception ex)
            {
                output.WriteLine($"Произошла неожиданная ошибка во время неудачной попытки: {ex.Message}");
            }
        }


        output.WriteLine("Попытка обработки с выключенным RabbitMQ завершена.");

        await using (var checkScope = factory.Services.CreateAsyncScope())
        {
            var dbContext = checkScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var messageAfterFailedAttempt = await dbContext.OutboxMessages.AsNoTracking().SingleAsync();
            messageAfterFailedAttempt.ProcessedAt.Should()
                .BeNull("сообщение не должно быть помечено как обработанное, так как брокер был недоступен");
        }

        output.WriteLine("Проверка подтвердила: сообщение осталось необработанным в Outbox.");

        // ACT & ASSERT (Часть 2) - Шаг 3: Восстанавливаем RabbitMQ
        output.WriteLine("\nШаг 3: Восстановление RabbitMQ и успешная доставка сообщения...");
        await factory.RabbitMqContainer.StartAsync();
        output.WriteLine(
            $"Контейнер RabbitMQ запущен. Host: {factory.RabbitMqContainer.Hostname}, Port: {factory.RabbitMqContainer.GetMappedPublicPort(5672)}");
        var factoryAfterRestart = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IConnection>();
                var newConnectionFactory = new ConnectionFactory
                {
                    HostName = factory.RabbitMqContainer.Hostname,
                    Port = factory.RabbitMqContainer.GetMappedPublicPort(5672),
                    UserName = "guest",
                    Password = "guest",
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };
                var newConnection = newConnectionFactory.CreateConnectionAsync().GetAwaiter().GetResult();
                services.AddSingleton(newConnection);
            });
        });


        var tcs = new TaskCompletionSource<string>();
        var listenerConnectionFactory = new ConnectionFactory
        {
            HostName = factory.RabbitMqContainer.Hostname, Port = factory.RabbitMqContainer.GetMappedPublicPort(5672),
            UserName = "guest", Password = "guest"
        };
        await using var listenerConnection = await listenerConnectionFactory.CreateConnectionAsync();
        await using var channel = await listenerConnection.CreateChannelAsync();
        await channel.QueueDeclareAsync("account.crm.test", durable: false, exclusive: true, autoDelete: true);
        await channel.QueueBindAsync("account.crm.test", "account.events", "account.opened");

        var consumer = new AsyncEventingBasicConsumer(channel);


        consumer.ReceivedAsync += (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                // Пытаемся десериализовать сообщение как наше событие
                var receivedEvent = JsonSerializer.Deserialize<EventEnvelope<object>>(message);

                // Если это наше событие и ID совпадает, завершаем задачу
                if (receivedEvent != null && receivedEvent.EventId == eventId)
                {
                    output.WriteLine($"!!! Пойман правильный EventId: {receivedEvent.EventId}. Обрабатываем.");
                    tcs.TrySetResult(message);
                }
                else
                {
                    // Это другое событие, игнорируем его
                    output.WriteLine($"--- Игнорируем сообщение с другим EventId: {receivedEvent?.EventId}");
                }
            }
            catch (JsonException)
            {
                output.WriteLine($"--- Игнорируем невалидное JSON сообщение: {message}");
            }

            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(queue: "account.crm.test", autoAck: true, consumer: consumer);

        // Запускаем обработчик Outbox
        await using (var successScope = factoryAfterRestart.Services.CreateAsyncScope())
        {
            var outboxProcessor = successScope.ServiceProvider.GetRequiredService<OutboxProcessorJob>();
            await outboxProcessor.ProcessOutboxMessagesAsync(new JobCancellationToken(false));
        }

        // Ждем получения сообщения 
        var receivedMessageTask = tcs.Task;
        var completedTask =
            await Task.WhenAny(receivedMessageTask,
                Task.Delay(TimeSpan.FromSeconds(15)));
        completedTask.Should().Be(receivedMessageTask, "ожидаемое сообщение должно быть получено из очереди");

        var receivedPayload = await receivedMessageTask; 
        
        output.WriteLine($"Получено и обработано финальное сообщение: {receivedPayload}");


        
        
        var receivedEnvelope = JsonSerializer.Deserialize<EventEnvelope<AccountOpenedEvent>>(receivedPayload);
        receivedEnvelope.Should().NotBeNull();
        receivedEnvelope.EventId.Should().Be(eventId);
        receivedEnvelope.Payload.OwnerId.Should().Be(ownerId);

        await using var dbCheckScope = factoryAfterRestart.Services.CreateAsyncScope();
        var context = dbCheckScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var messageAfterSuccess = await context.OutboxMessages.AsNoTracking().SingleAsync(m => m.Id == eventId);
        messageAfterSuccess.ProcessedAt.Should().NotBeNull();

        output.WriteLine("Тест завершен успешно.");
    }
}