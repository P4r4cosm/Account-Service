using System.Data;
using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using AccountService.Shared.Providers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    IOutboxMessageRepository outboxMessageRepository,
    ICorrelationIdProvider correlationIdProvider,
    ILogger<UpdateAccountHandler> logger,
    IClientVerificationService clientVerificationService)
    : IRequestHandler<UpdateAccountCommand, MbResult>
{
    public async Task<MbResult> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

            var validationResult = await ValidateAccountAsync(account, request, cancellationToken);
            if (validationResult.IsFailure)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return validationResult;
            }

            // account точно не null т.к. отработала валидация
            var oldOwnerId = account!.OwnerId;
            var oldInterestRate = account.InterestRate;
            var wasPreviouslyClosed = account.CloseDate.HasValue;


            account.OwnerId = request.OwnerId;
            account.InterestRate = request.InterestRate; // Если в запросе придет null, поле обнулится.
            account.CloseDate = request.CloseDate; // Если в запросе придет null, дата закрытия сотрется.

            var eventsToPublish = new List<object>();
            if (account.OwnerId != oldOwnerId)
            {
                eventsToPublish.Add(new AccountOwnerChangedEvent
                    { AccountId = account.Id, OldOwnerId = oldOwnerId, NewOwnerId = account.OwnerId });
            }

            if (account.InterestRate != oldInterestRate)
            {
                eventsToPublish.Add(new AccountInterestRateChangedEvent
                    { AccountId = account.Id, OldRate = oldInterestRate, NewRate = account.InterestRate });
            }

            var isNowClosed = account.CloseDate.HasValue;
            switch (isNowClosed)
            {
                // Был открыт, стал закрыт
                case true when !wasPreviouslyClosed:
                    eventsToPublish.Add(new AccountClosedEvent
                        { AccountId = account.Id, OwnerId = account.OwnerId, ClosedAt = account.CloseDate!.Value });
                    break;
                // Был закрыт, стал открыт
                case false when wasPreviouslyClosed:
                    eventsToPublish.Add(new AccountReopenedEvent { AccountId = account.Id, OwnerId = account.OwnerId });
                    break;
            }

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


            // Фиксируем все изменения в базе данных
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Конфликт параллельного доступа при обновлении счёта {AccountId}", request.AccountId);
            return MbResult.Failure(MbError.Custom("Account.Conflict",
                "Не удалось обновить счёт, так как его данные были изменены. Пожалуйста, обновите информацию и попробуйте снова."));
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Ошибка базы данных при обновлении счёта {AccountId}", request.AccountId);
            return MbResult.Failure(MbError.Custom("Database.DbError",
                "Произошла ошибка при сохранении изменений в базу данных."));
        }

        return MbResult.Success();
    }

    private async Task<MbResult> ValidateAccountAsync(Account? account, UpdateAccountCommand request,
        CancellationToken cancellationToken)
    {
        if (account is null)
        {
            return MbResult.Failure(MbError.Custom("Account.NotFound", $"Счёт {request.AccountId} не найден."));
        }

        // Проверяем существование нового владельца
        if (!await clientVerificationService.ClientExistsAsync(request.OwnerId, cancellationToken))
        {
            return MbResult.Failure(MbError.Custom("Client.NotFound", $"Клиент {request.OwnerId} не найден."));
        }

        // Проверяем бизнес-правило: можно ли установить ставку на этот тип счёта
        if (request.InterestRate.HasValue && account.AccountType is AccountType.Checking)
        {
            return MbResult.Failure(MbError.Custom("Account.Update.Forbidden",
                "Установка процентной ставки невозможна для текущих счетов."));
        }

        // Проверяем бизнес-правило: дата закрытия
        if (request.CloseDate.HasValue && request.CloseDate.Value < account.OpenedDate)
        {
            return MbResult.Failure(MbError.Custom("Account.Validation",
                "Дата закрытия не может быть раньше даты открытия."));
        }

        return MbResult.Success();
    }
}