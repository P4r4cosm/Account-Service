using Hangfire;

namespace AccountService.Infrastructure.Persistence.Interfaces;

public interface IInterestAccrualService
{
    Task AccrueInterestForAllDepositsAsync(IJobCancellationToken cancellationToken);
    
}