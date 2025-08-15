// using AccountService.Shared.Options;
// using Microsoft.EntityFrameworkCore.Metadata;
// using Microsoft.Extensions.Options;
// using RabbitMQ.Client;
// using RabbitMQ.Client.Events;
//
// namespace AccountService.Shared.MessageBroker;
//
// public class AccountServiceConsumer: BackgroundService
// {
//     
//     private readonly ILogger<AccountServiceConsumer> _logger;
//     private readonly RabbitMqOptions _rabbitMqOptions;
//     private readonly IConnection _connection;
//     private readonly IChannel _channel;
//     
//     
//     public  AccountServiceConsumer(ILogger<AccountServiceConsumer> logger, IOptions<RabbitMqOptions> options)
//     {
//         _rabbitMqOptions = options.Value;
//         _logger = logger;
//         var factory = new ConnectionFactory()
//         {
//             HostName = _rabbitMqOptions.HostName,
//             Port = _rabbitMqOptions.Port,
//             VirtualHost = _rabbitMqOptions.VirtualHost,
//             UserName = _rabbitMqOptions.UserName,
//             Password = _rabbitMqOptions.Password,
//         };
//         _connection =  factory.CreateConnectionAsync().GetAwaiter().GetResult();
//         _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
//     }
//         
//     protected override Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         stoppingToken.ThrowIfCancellationRequested();
//         var consumer = new AsyncEventingBasicConsumer(_channel);
//
//         // consumer.ReceivedAsync += (_, ea) =>
//         // {
//         //
//         // };
//         return Task.CompletedTask;
//     }
// }