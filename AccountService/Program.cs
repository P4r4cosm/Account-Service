using System.Net.Sockets;
using System.Text.Json.Serialization;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Persistence.HangfireServices;
using AccountService.Infrastructure.Persistence.HealthChecks;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Persistence.MessageBroker;
using AccountService.Infrastructure.Persistence.Repositories;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Behaviors;
using AccountService.Shared.Domain;
using AccountService.Shared.Extensions;
using AccountService.Shared.Filters;
using AccountService.Shared.Middleware;
using AccountService.Shared.Options;
using AccountService.Shared.Providers;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Serilog;
using Serilog.Events;


namespace AccountService;

public class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting web application");

            var builder = WebApplication.CreateBuilder(args);

            await ConfigureServices(builder);

            var app = builder.Build();

            await ConfigureMiddlewareAsync(app);

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync(); // Очень важно! Гарантирует, что все логи будут записаны перед выходом.
        }
    }

    private static async Task ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration) // Читаем конфигурацию из appsettings.json
                .ReadFrom.Services(services) // Позволяет внедрять зависимости в компоненты Serilog
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "AccountService") // Добавляем статическое поле ко всем логам
        );

        // Регистрируем IHttpContextAccessor, чтобы иметь доступ к HttpContext из сервисов
        builder.Services.AddHttpContextAccessor();

        // Регистрируем наш провайдер как Scoped (он будет жить в рамках одного HTTP-запроса)
        builder.Services.AddScoped<ICorrelationIdProvider, CorrelationIdProvider>();


        //Options
        builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

        //RabbitMq
        var rabbitMqOptions = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqOptions>();
        if (rabbitMqOptions is null)
            throw new InvalidOperationException("RabbitMQ configuration is missing or invalid.");
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqOptions.HostName,
            Port = rabbitMqOptions.Port,
            UserName = rabbitMqOptions.UserName,
            Password = rabbitMqOptions.Password,
            VirtualHost = rabbitMqOptions.VirtualHost,
            AutomaticRecoveryEnabled = true,
            // Опционально: интервал между попытками переподключения.
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10) 
        };

        // Создаем политику Polly для повторного подключения к RabbitMQ при старте.
        var retryPolicy = Policy
            // Указываем, какие исключения мы хотим обрабатывать
            .Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            // Определяем стратегию повторов: 10 попыток с экспоненциальной задержкой
            .WaitAndRetryAsync(
                retryCount: 10,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, _) =>
                {
                    // Логируем каждую попытку переподключения. Это очень важно для диагностики.
                    Log.Warning(
                        "Не удалось подключиться к RabbitMQ. Попытка {RetryCount} через {TimeSpan}. Причина: {Exception}",
                        retryCount, timeSpan, exception.Message);
                });
        var connection = await retryPolicy.ExecuteAsync(() => factory.CreateConnectionAsync());
        Log.Information("Успешное подключение к RabbitMQ Host: {HostName}", rabbitMqOptions.HostName);

        builder.Services.AddSingleton(connection);

        builder.Services.AddHostedService<RabbitMqInitializer>();
        builder.Services.AddSingleton<IMessagePublisher, RabbitMqMessagePublisher>();

        // Controllers + JSON
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        // Stub services
        builder.Services.AddSingleton<IClientVerificationService, StubClientVerificationService>();
        builder.Services.AddSingleton<ICurrencyService, StubCurrencyService>();


        // Hangfire services
        builder.Services.AddScoped<IInterestAccrualService, InterestAccrualService>();
        builder.Services.AddScoped<IInterestAccrualOrchestrator, InterestAccrualOrchestrator>();


        builder.Services.AddScoped<OutboxProcessorJob>(); // Регистрируем нашу задачу как Scoped

        // PostgreSQL
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
        // Repository + UnitOfWork
        builder.Services.AddScoped<IAccountRepository, PostgresAccountRepository>();
        builder.Services.AddScoped<ITransactionRepository, PostgresTransactionRepository>();
        builder.Services.AddScoped<IOutboxMessageRepository, PostgresOutboxMessageRepository>();
        builder.Services.AddScoped<IInboxRepository, PostgresInboxRepository>();
        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Регистрация консьюмера как фонового сервиса (Hosted Service)
        builder.Services.AddHostedService<AntifraudConsumer>();

        // Регистрируем кастомную проверку в DI-контейнере
        builder.Services.AddScoped<OutboxHealthCheck>();


        builder.Services.AddAllHealthChecks(builder.Configuration);

        builder.Services.AddHealthChecksUI(options =>
            {
                options.AddHealthCheckEndpoint("Сервис Счетов (Account Service)", "/health/ready");
                options.SetEvaluationTimeInSeconds(10);
            })
            .AddInMemoryStorage();

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy => { policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); });
        });


        // 1. Настройка HangFire
        builder.Services.AddHangfire(configuration => configuration
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"));
            }));

        // 2. Добавление фонового обработчика HangFire
        builder.Services.AddHangfireServer();

        // Auth & Authorization
        if (builder.Environment.IsEnvironment("Testing"))
        {
            // Для среды "Testing" мы заменяем стандартную политику авторизации
            // на политику, которая разрешает все запросы.
            builder.Services.AddAuthorizationBuilder()
                // Для среды "Testing" мы заменяем стандартную политику авторизации
                // на политику, которая разрешает все запросы.
                .SetDefaultPolicy(new AuthorizationPolicyBuilder()
                    .RequireAssertion(_ => true) // Всегда разрешать
                    .Build());
        }
        else
        {
            // Для всех остальных сред (Development, Production) настраиваем
            // стандартную JWT Bearer аутентификацию и авторизацию.
            builder.Services.AddAuthenticationBearer(builder.Configuration);
            builder.Services.AddAuthorization();
        }

        // FluentValidation
        ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
        builder.Services.AddValidatorsFromAssemblyContaining<Program>();

        // AutoMapper
        builder.Services.AddAutoMapper(_ => { }, typeof(Program));

        // Mediatr
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGetWithAuth(builder.Configuration);
    }


    private static async Task ConfigureMiddlewareAsync(WebApplication app)
    {
        app.UseSerilogRequestLogging();

        // DB migrations
        await app.MigrateDatabaseAsync<ApplicationDbContext>();

        // CORS
        app.UseCors("AllowAll");

        app.UseMiddleware<CorrelationIdMiddleware>();

        // Swagger in dev
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Service API V1");
                options.SwaggerEndpoint("/swagger/events-v1/swagger.json", "События v1");
                options.OAuthClientId(app.Configuration["Swagger:ClientId"]);
            });
        }

        // Auth - Теперь эти вызовы безопасны для всех сред.
        // Условная логика больше не нужна.
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = [new HangfireAuthorizationFilter()]
        });

        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui"; // Путь к дашборду
        });
        // Controllers
        app.MapControllers();
        // app.MapControllers().RequireAuthorization(); 

        RecurringJob.AddOrUpdate<IInterestAccrualOrchestrator>(
            "daily-interest-accrual",
            orchestrator => orchestrator.StartAccrualProcess(),
            Cron.Daily,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

        RecurringJob.AddOrUpdate<OutboxProcessorJob>(
            "process-outbox-messages", // Уникальный идентификатор задачи
            job => job.ProcessOutboxMessagesAsync(JobCancellationToken.Null), // Метод, который нужно вызывать
            "*/10 * * * * *", // CRON-выражение для запуска каждые 10 секунд
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}