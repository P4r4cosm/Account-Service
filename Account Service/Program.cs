using System.Reflection;
using System.Text.Json.Serialization;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Behaviors;
using AccountService.Shared.Filters;
using FluentValidation;
using MediatR;

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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Account Service API",
        Description =
            "Микросервис для управления банковскими счетами и транзакциями в соответствии с заданием Модуль Банка."
    });

    // Включаем отображение комментариев в интерфейсе Swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

// 2. Построение приложения
var app = builder.Build();

// 3. Конфигурация конвейера обработки HTTP-запросов (Middleware)

// Включаем Swagger только в режиме разработки для безопасности
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        // Чтобы Swagger UI открывался по корневому URL (http://localhost:xxxx/)
        options.RoutePrefix = string.Empty;
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Service API V1");
    });
}

app.UseHttpsRedirection(); // Перенаправляет HTTP на HTTPS

app.MapControllers(); // Сопоставляет запросы с методами контроллеров

app.Run(); // Запускает приложение