using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    // сериализация enum в строку
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
//automapper
builder.Services.AddAutoMapper(cfg => { },
    typeof(Program));
//swagger
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
{
    Version = "v1",
    Title = "Account Service API",
    Description =
        "Микросервис для управления банковскими счетами и транзакциями в соответствии с заданием Модуль Банка."
}));
var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwaggerUI();
app.UseSwagger();
app.MapOpenApi();


app.MapControllers();

app.Run();