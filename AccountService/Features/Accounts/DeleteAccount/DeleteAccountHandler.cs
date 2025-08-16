using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using AccountService.Shared.Providers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(IAccountRepository accountRepository, IUnitOfWork unitOfWork,
    IOutboxMessageRepository outboxMessageRepository,
    ICorrelationIdProvider correlationIdProvider,
    ILogger<DeleteAccountHandler> logger) : IRequestHandler<DeleteAccountCommand, MbResult>
{
    public async Task<MbResult> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId,cancellationToken);
        if (account == null)
        {
            return MbResult.Failure(
                MbError.Custom("Account.NotFound", $"Счёт {request.AccountId} не найден."));
        }
        await accountRepository.DeleteAsync(account,cancellationToken);

        var accountClosedEvent = new AccountClosedEvent
        {
            AccountId = account.Id,
            OwnerId = account.OwnerId,
            ClosedAt = DateTime.UtcNow
        };
        var correlationId = correlationIdProvider.GetCorrelationId();
        var causationId = request.CommandId;
        var eventEnvelope = new EventEnvelope<AccountClosedEvent>(accountClosedEvent, correlationId, causationId);
        var outboxMessage = new OutboxMessage
        {
            Id = eventEnvelope.EventId,
            Type = nameof(AccountClosedEvent), // Безопасное получение имени типа
            Payload = JsonSerializer.Serialize(eventEnvelope),
            OccurredAt = eventEnvelope.OccurredAt,
            CorrelationId = correlationId
        };
        outboxMessageRepository.Add(outboxMessage);
        
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken); 
        }
        // Перехватываем ошибку параллельного доступа (оптимистическая блокировка)
        catch (DbUpdateConcurrencyException ex)
        {
            // Это ожидаемый конфликт, а не ошибка сервера. Логгируем как Warning.
            logger.LogWarning(ex, "Конфликт параллельного доступа при попытке удалить счёт {AccountId}", request.AccountId);
            
            // Возвращаем клиенту ошибку, которая превратится в 409 Conflict
            return MbResult.Failure(
                MbError.Custom("Account.Conflict", "Не удалось удалить счёт, так как его данные были изменены. Пожалуйста, обновите информацию и попробуйте снова."));
        }
        // Перехватываем другие ошибки обновления базы данных
        catch (DbUpdateException ex)
        {
            // Это неожиданная ошибка. Логгируем как Error.
            logger.LogError(ex, "Ошибка базы данных при попытке удалить счёт {AccountId}", request.AccountId);
            
            // Возвращаем клиенту общую ошибку, которая превратится в 500 Internal Server Error
            return MbResult.Failure(
                MbError.Custom("Database.DbError", "Произошла ошибка при сохранении изменений в базу данных."));
        }
        
        // 4. Если всё прошло успешно
        return MbResult.Success();

    }
}