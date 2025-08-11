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


    // Переименовываем и изменяем сигнатуру старого метода
    public async Task AccrueInterestForBatchAsync(int pageNumber, int pageSize, IJobCancellationToken cancellationToken)
    {
        var token = cancellationToken.ShutdownToken;
        logger.LogInformation("Начало обработки батча #{PageNumber}.", pageNumber);
        
        await unitOfWork.BeginTransactionAsync(IsolationLevel.RepeatableRead, token);
        try
        {
            var accountIds = (List<Guid>)await accountRepository.GetPagedAccountIdsForAccrueInterestAsync(pageNumber, pageSize, token) ;
            
            foreach (var id in accountIds)
            {
                token.ThrowIfCancellationRequested();
                await accountRepository.AccrueInterest(id, token);
            }

            await unitOfWork.CommitTransactionAsync(token);
            logger.LogInformation("Батч #{PageNumber} успешно обработан. Счетов: {Count}", pageNumber, accountIds.Count);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Обработка батча #{PageNumber} была отменена. Откат транзакции.", pageNumber);
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Критическая ошибка при обработке батча #{PageNumber}. Откат транзакции.", pageNumber);
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw; // Важно пробросить, чтобы Hangfire пометил этот батч как Failed
        }
    }
    
}