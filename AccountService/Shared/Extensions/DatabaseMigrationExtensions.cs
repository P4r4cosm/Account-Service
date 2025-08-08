using Microsoft.EntityFrameworkCore;

namespace AccountService.Shared.Extensions;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync<TDbContext>(this WebApplication app) where TDbContext : DbContext
    {
        // Создаем using scope для получения доступа к сервисам
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            
            // Получаем логгер для записи информации или ошибок
            var logger = services.GetRequiredService<ILogger<TDbContext>>();
            // Получаем экземпляр нашего DbContext
            var dbContext = services.GetRequiredService<TDbContext>();

            try
            {
                logger.LogInformation("Применение миграций базы данных для контекста {DbContextName}", typeof(TDbContext).Name);

                // Асинхронно применяем все ожидающие миграции
                await dbContext.Database.MigrateAsync();

                logger.LogInformation("Миграции для контекста {DbContextName} успешно применены", typeof(TDbContext).Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Произошла ошибка при применении миграций для контекста {DbContextName}", typeof(TDbContext).Name);
                // В зависимости от критичности, можно остановить приложение
                // throw; 
            }
        }
    }
}