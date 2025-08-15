using AccountService.Shared.Domain;

namespace AccountService.Infrastructure.Persistence.Interfaces;


public interface IOutboxMessageRepository
{
    /// <summary>
    /// Добавляет исходящее сообщение в хранилище для последующей отправки.
    /// Этот метод только помечает сущность как добавленную; фактическое сохранение
    /// происходит при вызове IUnitOfWork.SaveChangesAsync().
    /// </summary>
    /// <param name="message">Сообщение для добавления.</param>
    void Add(OutboxMessage message);
    
  
}