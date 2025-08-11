using System.Text.Json.Serialization;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Persistence.HangfireServices;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Persistence.Repositories;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Behaviors;
using AccountService.Shared.Domain;
using AccountService.Shared.Extensions;
using AccountService.Shared.Filters;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AccountService;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder);

        var app = builder.Build();

        await ConfigureMiddlewareAsync(app);

        await app.RunAsync();
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        // Controllers + JSON
        builder.Services.AddControllers(options => { options.Filters.Add<ApiExceptionFilter>(); })
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

        // PostgreSQL
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Repository + UnitOfWork
        builder.Services.AddScoped<IAccountRepository, PostgresAccountRepository>();
        builder.Services.AddScoped<ITransactionRepository, PostgresTransactionRepository>();
        builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());

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
        // DB migrations
        await app.MigrateDatabaseAsync<ApplicationDbContext>();
        
        // CORS
        app.UseCors("AllowAll");

        // Swagger in dev
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Service API V1");
                options.OAuthClientId(app.Configuration["Swagger:ClientId"]);
            });
        }

        // Auth - Теперь эти вызовы безопасны для всех сред.
        // Условная логика больше не нужна.
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() }
        });
        // Controllers
        app.MapControllers();
        // app.MapControllers().RequireAuthorization(); 
        
        RecurringJob.AddOrUpdate<IInterestAccrualOrchestrator>(
            "daily-interest-accrual",
            orchestrator => orchestrator.StartAccrualProcess(),
            Cron.Daily,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
