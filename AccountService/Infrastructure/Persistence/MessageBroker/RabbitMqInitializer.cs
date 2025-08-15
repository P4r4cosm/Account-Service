using AccountService.Shared.Options;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AccountService.Infrastructure.Persistence.MessageBroker;

public class RabbitMqInitializer(IConnection connection, IOptions<RabbitMqOptions> options,ILogger<RabbitMqInitializer> logger)
    : IHostedService
{
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Запускается асинхронная инициализация топологии RabbitMQ...");

        // Создаем временный асинхронный канал для операций
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        var exchangeName = options.Value.ExchangeName;
        // Объявляем Exchange типа 'topic'
        await channel.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Topic,
            durable: true, cancellationToken: cancellationToken);
        logger.LogInformation("Exchange {exchangeName} успешно объявлен.", exchangeName);

        // Объявляем очереди
        await channel.QueueDeclareAsync("account.crm", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync("account.notifications", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync("account.antifraud", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync("account.audit", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        logger.LogInformation("Очереди успешно объявлены.");

        // Привязываем очереди к Exchange
        await channel.QueueBindAsync("account.crm", exchangeName, "account.*", cancellationToken: cancellationToken);
        await channel.QueueBindAsync("account.notifications", exchangeName, "money.*", cancellationToken: cancellationToken);
        await channel.QueueBindAsync("account.antifraud", exchangeName, "client.*", cancellationToken: cancellationToken);
        await channel.QueueBindAsync("account.audit", exchangeName, "#", cancellationToken: cancellationToken);
        logger.LogInformation("Привязки очередей к exchange {exchangeName} успешно созданы.", exchangeName);
        
        logger.LogInformation("Инициализация топологии RabbitMQ завершена.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}