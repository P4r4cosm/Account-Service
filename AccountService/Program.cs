using System.Text.Json.Serialization;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Behaviors;
using AccountService.Shared.Extensions;
using AccountService.Shared.Filters;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Конфигурация сервисов (Dependency Injection)

// Добавляем контроллеры и настраиваем глобальные фильтры и JSON
builder.Services.AddControllers(options => { options.Filters.Add<ApiExceptionFilter>(); })
    .AddJsonOptions(options =>
    {
        // Сериализация enum в строку
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Регистрируем сервисы-заглушки
builder.Services.AddSingleton<IClientVerificationService, StubClientVerificationService>();
builder.Services.AddSingleton<ICurrencyService, StubCurrencyService>();


builder.Services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();


// Postgres
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// === Добавляем CORS: Разрешаем все origins, методы и заголовки ===
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin() // Разрешить запросы с любого домена
            .AllowAnyMethod() // Разрешить все HTTP-методы (GET, POST, PUT, DELETE и т.д.)
            .AllowAnyHeader(); // Разрешить все заголовки
    });
});

builder.Services.AddAuthorization();
builder.Services.AddAuthenticationBearer(builder.Configuration);

// Устанавливаем глобальное правило: для каждого свойства (RuleFor)
// прекращать валидацию после первой же неудавшейся проверки.
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;

// Регистрируем FluentValidation и все валидаторы из сборки
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Регистрируем AutoMapper и все профили из сборки
builder.Services.AddAutoMapper(_ => { }, typeof(Program));

// Регистрируем Mediatr все его компоненты из сборки
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Использовать для любого IPipelineBehavior<TRequest, TResponse> ValidationBehavior
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Настраиваем Swagger (Swashbuckle)
builder.Services.AddEndpointsApiExplorer(); // Необходимо для Swagger
builder.Services.AddSwaggerGetWithAuth(builder.Configuration);

// 2. Построение приложения
var app = builder.Build();

// 3. Конфигурация конвейера обработки HTTP-запросов (Middleware)

// Включаем CORS — обязательно ДО других middleware, обрабатывающих запросы
app.UseCors("AllowAll");

// Включаем Swagger только в режиме разработки для безопасности
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        // Чтобы Swagger UI открывался по корневому URL (http://localhost:xxxx/)
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Service API V1");
        options.OAuthClientId(builder.Configuration["Swagger:ClientId"]);
    });
}

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers().RequireAuthorization(); // Сопоставляет запросы с методами контроллеров

app.Run(); // Запускает приложение