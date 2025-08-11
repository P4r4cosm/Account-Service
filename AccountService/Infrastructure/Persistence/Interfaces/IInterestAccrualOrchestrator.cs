using System.ComponentModel;

namespace AccountService.Infrastructure.Persistence.Interfaces;

public interface IInterestAccrualOrchestrator
{
    // Атрибут для того, чтобы в логах Hangfire было понятное имя
    [DisplayName("Начать процесс начисления процентов по вкладам (Оркестратор)")]
    Task StartAccrualProcess();
}