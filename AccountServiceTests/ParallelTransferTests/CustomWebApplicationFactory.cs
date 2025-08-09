using AccountService.Infrastructure.Persistence;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using IContainer = DotNet.Testcontainers.Containers.IContainer;
using Program = AccountService.Program;

namespace AccountServiceTests.ParallelTransferTests;

// ReSharper disable once ClassNeverInstantiated.Global Resharper считает, что нужно сделать класс абстрактным, но нужен создаваемый объект для ParallelTransferTests
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Создаём описание нашего будущего контейнера PostgreSQL
    private readonly IContainer _dbContainer = new ContainerBuilder()
        .WithImage("postgres:15-alpine") // Используем легковесный образ PostgreSQL
        .WithPortBinding(5432, true)      // Пробрасываем порт
        .WithEnvironment("POSTGRES_USER", "user")
        .WithEnvironment("POSTGRES_PASSWORD", "user")
        .WithEnvironment("POSTGRES_DB", "db")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432)) // Ждём, пока БД будет готова
        .Build();

    // Этот метод вызывается для настройки веб-хоста перед созданием HttpClient
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // 1. Убираем оригинальную регистрацию DbContext из приложения
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            // 2. Добавляем новую регистрацию, указывающую на нашу тестовую БД в контейнере
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                // Получаем строку подключения из запущенного контейнера
                var connectionString = $"Host={_dbContainer.Hostname};Port={_dbContainer.GetMappedPublicPort(5432)};Database=db;Username=user;Password=user;";
                options.UseNpgsql(connectionString);
            });
        });
    }

    // Метод из IAsyncLifetime, вызывается перед запуском тестов
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync(); // Запускаем контейнер с БД
    }

    // Метод из IAsyncLifetime, вызывается после завершения всех тестов
    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync(); // Останавливаем и удаляем контейнер
    }
}