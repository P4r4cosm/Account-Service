using System.Data;
using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using AccountService.Shared.Providers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Features.Accounts.PatchAccount;

public class PatchAccountHandler(
    IAccountRepository accountRepository,
    IClientVerificationService clientVerificationService,
    IUnitOfWork unitOfWork,
    IOutboxMessageRepository outboxMessageRepository,
    ICorrelationIdProvider correlationIdProvider,
    ILogger<PatchAccountHandler> logger)
    : IRequestHandler<PatchAccountCommand, MbResult>
{
    public async Task<MbResult> Handle(PatchAccountCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
            if (account is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return MbResult.Failure(MbError.Custom("Account.NotFound", $"Счёт {request.AccountId} не найден."));
            }

            var eventsToPublish = new List<object>();
            var hasChanges = false;

            // 1. Обновляем OwnerId, если он был передан и изменен
            if (request.OwnerId.HasValue && request.OwnerId.Value != account.OwnerId)
            {
                if (!await clientVerificationService.ClientExistsAsync(request.OwnerId.Value, cancellationToken))
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return MbResult.Failure(MbError.Custom("Owner.NotFound",
                        $"Клиент {request.OwnerId.Value} не найден"));
                }

                var oldOwnerId = account.OwnerId;
                account.OwnerId = request.OwnerId.Value;
                hasChanges = true;
                eventsToPublish.Add(new AccountOwnerChangedEvent
                    { AccountId = account.Id, OldOwnerId = oldOwnerId, NewOwnerId = account.OwnerId });
            }

            // 2. Обновляем InterestRate, если он был передан и изменен
            if (request.InterestRate.HasValue && request.InterestRate.Value != account.InterestRate)
            {
                if (account.AccountType is AccountType.Checking)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return MbResult.Failure(MbError.Custom("Account.Update.Forbidden",
                        "Установка процентной ставки недоступна для текущих счетов."));
                }

                var oldRate = account.InterestRate;
                account.InterestRate = request.InterestRate.Value;
                hasChanges = true;
                eventsToPublish.Add(new AccountInterestRateChangedEvent
                    { AccountId = account.Id, OldRate = oldRate, NewRate = account.InterestRate });
            }

            // 3. Обновляем дату закрытия, если она была передана и изменена
            // Этот блок теперь также обрабатывает повторное открытие счета, если пришел null
            if (request.CloseDate != account.CloseDate)
            {
                if (request.CloseDate.HasValue && request.CloseDate.Value < account.OpenedDate)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return MbResult.Failure(MbError.Custom("Account.Validation",
                        "Дата закрытия не может быть раньше даты открытия."));
                }

                var wasPreviouslyClosed = account.CloseDate.HasValue;
                account.CloseDate = request.CloseDate;
                hasChanges = true;
                var isNowClosed = account.CloseDate.HasValue;

                switch (isNowClosed)
                {
                    case true when !wasPreviouslyClosed:
                        eventsToPublish.Add(new AccountClosedEvent
                            { AccountId = account.Id, OwnerId = account.OwnerId, ClosedAt = account.CloseDate!.Value });
                        break;
                    case false when wasPreviouslyClosed:
                        eventsToPublish.Add(new AccountReopenedEvent { AccountId = account.Id, OwnerId = account.OwnerId });
                        break;
                }
            }

            // Если изменений не было, ничего не делаем
            if (!hasChanges)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return MbResult.Success();
            }

            // Сохраняем изменения в БД и Outbox
            await accountRepository.UpdateAsync(account, cancellationToken);

            var correlationId = correlationIdProvider.GetCorrelationId();
            var causationId = request.CommandId;
            for (var index = 0; index < eventsToPublish.Count; index++)
            {
                var domainEvent = eventsToPublish[index];
                var eventEnvelope = new EventEnvelope<object>(domainEvent, correlationId, causationId);
                var outboxMessage = new OutboxMessage
                {
                    Id = eventEnvelope.EventId,
                    Type = domainEvent.GetType().Name,
                    Payload = JsonSerializer.Serialize(eventEnvelope),
                    OccurredAt = eventEnvelope.OccurredAt,
                    CorrelationId = correlationId
                };
                outboxMessageRepository.Add(outboxMessage);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return MbResult.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Конфликт параллельного доступа при обновлении счёта {AccountId}",
                request.AccountId);
            return MbResult.Failure(MbError.Custom("Account.Conflict",
                "Не удалось обновить счёт, так как его данные были изменены. Пожалуйста, обновите информацию и попробуйте снова."));
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Ошибка базы данных при обновлении счёта {AccountId}", request.AccountId);
            return MbResult.Failure(MbError.Custom("Database.DbError",
                "Произошла ошибка при сохранении изменений в базу данных."));
        }
    }
}