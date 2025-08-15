using RabbitMQ.Client;

namespace AccountService.Shared.MessageBroker;

public class RabbitMqInitializer(IConnection connection, ILogger<RabbitMqInitializer> logger)
    : IHostedService
{
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Запускается асинхронная инициализация топологии RabbitMQ...");

        // Создаем временный асинхронный канал для операций
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        // Объявляем Exchange 'account.events' типа 'topic'
        await channel.ExchangeDeclareAsync(
            exchange: "account.events",
            type: ExchangeType.Topic,
            durable: true, cancellationToken: cancellationToken);
        logger.LogInformation("Exchange 'account.events' успешно объявлен.");

        // Объявляем очереди
        await channel.QueueDeclareAsync("account.crm", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync("account.notifications", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync("account.antifraud", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync("account.audit", durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        logger.LogInformation("Очереди успешно объявлены.");

        // Привязываем очереди к Exchange
        await channel.QueueBindAsync("account.crm", "account.events", "account.*", cancellationToken: cancellationToken);
        await channel.QueueBindAsync("account.notifications", "account.events", "money.*", cancellationToken: cancellationToken);
        await channel.QueueBindAsync("account.antifraud", "account.events", "client.*", cancellationToken: cancellationToken);
        await channel.QueueBindAsync("account.audit", "account.events", "#", cancellationToken: cancellationToken);
        logger.LogInformation("Привязки очередей к exchange 'account.events' успешно созданы.");
        
        logger.LogInformation("Инициализация топологии RabbitMQ завершена.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}