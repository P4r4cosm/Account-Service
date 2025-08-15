using AccountService.Infrastructure.Persistence.Interfaces;
using Hangfire;

namespace AccountService.Infrastructure.Persistence.HangfireServices;

public class InterestAccrualOrchestrator(
    IAccountRepository accountRepository, 
    IBackgroundJobClient backgroundJobClient, 
    ILogger<InterestAccrualOrchestrator> logger) : IInterestAccrualOrchestrator
{
    private const int BatchSize = 500;

    public async Task StartAccrualProcess()
    {
        logger.LogInformation("Запуск оркестратора начисления процентов. Размер батча: {BatchSize}", BatchSize);

        var totalAccounts = await accountRepository.GetAccountCountForAccrueInterestAsync(CancellationToken.None);

        if (totalAccounts == 0)
        {
            logger.LogInformation("Нет счетов для начисления процентов. Задача завершена.");
            return;
        }

        var totalBatches = (int)Math.Ceiling((double)totalAccounts / BatchSize);
        logger.LogInformation("Всего счетов: {TotalAccounts}. Будет создано {TotalBatches} задач.", totalAccounts, totalBatches);

        for (var i = 1; i <= totalBatches; i++)
        {
            var pageNumber = i;
            // Ставим в очередь задачу для обработки ОДНОГО батча.
            // Hangfire сам позаботится о ее выполнении.
            backgroundJobClient.Enqueue<IInterestAccrualService>(
                service => service.AccrueInterestForBatchAsync(pageNumber, BatchSize, null, JobCancellationToken.Null));
        }

        logger.LogInformation("Все {TotalBatches} задач для начисления процентов успешно поставлены в очередь.", totalBatches);
    }
}