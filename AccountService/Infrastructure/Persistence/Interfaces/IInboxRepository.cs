using AccountService.Shared.Domain;

namespace AccountService.Infrastructure.Persistence.Interfaces;

public interface IInboxRepository
{
    /// <summary>
    /// Проверяет, было ли сообщение с таким ID уже обработано указанным хендлером.
    /// </summary>
    Task<bool> IsHandledAsync(Guid messageId, string handlerName, CancellationToken cancellationToken);

    /// <summary>
    /// Добавляет запись об обработанном сообщении.
    /// Не вызывает SaveChanges, это должно происходить в рамках Unit of Work.
    /// </summary>
    void Add(InboxConsumedMessage message);

    /// <summary>
    /// Добавляет сообщение в "карантин" (dead-letter).
    /// </summary>
    void AddToDeadLetter(InboxDeadLetterMessage message);
}