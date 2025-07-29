var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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