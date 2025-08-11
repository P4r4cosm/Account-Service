using System.Data;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using Hangfire;

namespace AccountService.Infrastructure.Persistence.HangfireServices;

public class InterestAccrualService(
    IAccountRepository accountRepository,
    ILogger<InterestAccrualService> logger,
    IUnitOfWork unitOfWork)
    : IInterestAccrualService
{

    public async Task AccrueInterestForAllDepositsAsync(IJobCancellationToken cancellationToken)
    {
        var token = cancellationToken.ShutdownToken;
        logger.LogInformation("Запуск ежедневного начисления процентов по вкладам.");

        // Начинаем одну большую транзакцию для всех операций
        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, token);
        
        try
        {
            // Получаем все ID счетов, которым нужно начислить проценты
            var accountsIds = await accountRepository.GetAccountIdsForAccrueInterestAsync(token);
            logger.LogInformation("Найдено {Count} счетов для начисления процентов.", accountsIds.Count);

            // Проверяем, не была ли запрошена отмена, ПЕРЕД началом цикла
            token.ThrowIfCancellationRequested();

            
            foreach (var id in accountsIds)
            {
                
                token.ThrowIfCancellationRequested();

                logger.LogDebug("Начисление процентов для счета {AccountId}", id);
                
                await accountRepository.AccrueInterest(id, token);
            }

            // Если весь цикл прошел успешно, коммитим транзакцию
            await unitOfWork.CommitTransactionAsync(token);

            logger.LogInformation("Завершено ежедневное начисление процентов. Обработано {Count} счетов.", accountsIds.Count());
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Задача начисления процентов была отменена. Откатываем транзакцию.");
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None); 
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Произошла критическая ошибка при начислении процентов. Откатываем транзакцию.");
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None); 
            throw; // Пробрасываем, чтобы Hangfire пометил задачу как Failed
        }
    }
    
}