using AccountService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace AccountServiceTests.IntegrationTests.RabbitMqTests;

// ReSharper disable once ClassNeverInstantiated.Global Resharper требует сделать класс абстрактным, но он используется в тестах
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithDatabase("test_db")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .WithImage("postgres:15")
        .Build();

    // Создаем конфигурации для наших контейнеров
    // Стандартные учетные данные для тестов

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Этот метод вызывается при построении приложения.
        // Здесь мы заменяем реальные сервисы и конфигурацию на тестовые.
        builder.UseEnvironment("Testing");
        
       
        builder.ConfigureTestServices(services =>
        {
            
           
            // Удаляем зарегистрированный DbContext, чтобы подменить его
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            // Добавляем DbContext, который будет использовать строку подключения от нашего тестового контейнера PostgreSQL
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });
            

            // То же самое для RabbitMQ: подменяем IConnection
            services.RemoveAll<IConnection>();

            var factory = new ConnectionFactory
            {
                HostName = RabbitMqContainer.Hostname,
                Port = RabbitMqContainer.GetMappedPublicPort(5672), // Используем порт, который Docker назначил контейнеру
                UserName = "guest",
                Password = "guest"
            };
            var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            services.AddSingleton(connection);
        });
        builder.ConfigureLogging(loggingBuilder =>
        {
            // 1. Полностью удаляем всех провайдеров логирования,
            //    включая Serilog, настроенный в Program.cs
            loggingBuilder.ClearProviders();
        });
    }

    // Эти методы будут вызваны xUnit до и после выполнения тестов
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
        await RabbitMqContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await RabbitMqContainer.StopAsync();
    }
    
    // Публичное свойство для доступа к контейнеру из теста
    public RabbitMqContainer RabbitMqContainer { get; } = new RabbitMqBuilder()
        .WithImage("rabbitmq:3.12-management")
        .WithUsername("guest") // Стандартные учетные данные для тестов
        .WithPassword("guest")
        .Build();
}