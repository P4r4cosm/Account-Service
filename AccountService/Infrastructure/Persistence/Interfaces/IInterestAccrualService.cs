using System.ComponentModel;
using Hangfire;
using Hangfire.Server;

namespace AccountService.Infrastructure.Persistence.Interfaces;

public interface IInterestAccrualService
{
    // Атрибут для того, чтобы в логах Hangfire было понятное имя
    [DisplayName("Начисление процентов для батча #{0} (размер: {1})")]
    Task AccrueInterestForBatchAsync(int pageNumber, int pageSize,  PerformContext? performContext, IJobCancellationToken cancellationToken);
    
}