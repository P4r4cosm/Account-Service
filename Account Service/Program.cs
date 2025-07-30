using System.Reflection;
using System.Text.Json.Serialization;
using AccountService.Filters;
using AccountService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);


//фильтр для обработки исключений
builder.Services.AddControllers(options => { options.Filters.Add<ApiExceptionFilter>(); })
    .AddJsonOptions(options =>
    {
        // сериализация enum в строку
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

//acc repository
builder.Services.AddSingleton<IAccountRepository, InMemoryAccountRepository>();
//automapper
builder.Services.AddAutoMapper(cfg => { },
    typeof(Program));
//mediatr
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

//swagger
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    //включаем отображение комментариев в интерфейсе swagger
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);


    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Version = "v1",
        Title = "Account Service API",
        Description =
            "Микросервис для управления банковскими счетами и транзакциями в соответствии с заданием Модуль Банка."
    });
});
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwaggerUI();
app.UseSwagger();
app.MapOpenApi();


app.MapControllers();

app.Run();