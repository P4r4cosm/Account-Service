using System.Data;
using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using AccountService.Shared.Providers;
using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Features.Transactions.RegisterTransaction;

public class RegisterTransactionHandler(
    IAccountRepository accountRepository,
    ILogger<RegisterTransactionHandler> logger,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork,
    IOutboxMessageRepository outboxMessageRepository,
    ICorrelationIdProvider correlationIdProvider,
    IMapper mapper)
    : IRequestHandler<RegisterTransactionCommand, MbResult<TransactionDto>>
{
    public async Task<MbResult<TransactionDto>> Handle(RegisterTransactionCommand request,
        CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        try
        {
            var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
            if (account is null)
                return MbResult<TransactionDto>.Failure(MbError.Custom("Account.NotFound",
                    $"Счёт {request.AccountId} не найден."));

            if (account.CloseDate.HasValue)
                return MbResult<TransactionDto>.Failure(MbError.Custom("Account.Validation",
                    "Операции по закрытому счёту невозможны."));
            if (account.IsFrozen && Enum.Parse<TransactionType>(request.Type)==TransactionType.Debit)
                return MbResult<TransactionDto>.Failure(MbError.Custom("Account.Validation",
                    $"Счёт {request.AccountId} заморожен, операции снятия средств невозможны."));
            
            // Создаем новую сущность транзакции
            var newTransaction = mapper.Map<Transaction>(request);
            newTransaction.Id = Guid.NewGuid();
            newTransaction.AccountId = account.Id;
            newTransaction.Currency = account.Currency;
            newTransaction.Timestamp = DateTime.UtcNow;

            // Обновляем баланс и проверяем на достаточность средств
            if (newTransaction.Type == TransactionType.Debit && account.Balance < request.Amount)
                return MbResult<TransactionDto>.Failure(MbError.Custom("Transaction.Validation",
                    "Недостаточно средств на счёте для списания."));

            account.Balance += newTransaction.Type == TransactionType.Credit ? request.Amount : -request.Amount;
            
            
            var causationId = request.CommandId;
            var correlationId = correlationIdProvider.GetCorrelationId();
            string payloadJson;
            string eventType;
            Guid eventId;
            DateTime occurredAt;

            if (newTransaction.Type == TransactionType.Credit)
            {
                // Создаем DTO с полезной нагрузкой
                var moneyCreditedPayload = new MoneyCreditedEvent
                {
                    AccountId = newTransaction.AccountId,
                    Currency = newTransaction.Currency,
                    Amount = newTransaction.Amount,
                    OperationId = newTransaction.Id
                };

                //  Оборачиваем в "конверт"
                var eventEnvelope =
                    new EventEnvelope<MoneyCreditedEvent>(moneyCreditedPayload, correlationId, causationId);

                //  Заполняем наши переменные
                payloadJson = JsonSerializer.Serialize(eventEnvelope);
                eventType = nameof(MoneyCreditedEvent);
                eventId = eventEnvelope.EventId;
                occurredAt = eventEnvelope.OccurredAt;
            }
            else // TransactionType.Debit
            {
                //  Создаем DTO с полезной нагрузкой
                var moneyDebitedPayload = new MoneyDebitedEvent
                {
                    AccountId = newTransaction.AccountId,
                    Currency = newTransaction.Currency,
                    Amount = newTransaction.Amount,
                    OperationId = newTransaction.Id,
                    Reason = newTransaction.Description 
                };

                //  Оборачиваем в "конверт"
                var eventEnvelope =
                    new EventEnvelope<MoneyDebitedEvent>(moneyDebitedPayload, correlationId, causationId);

                //  Заполняем наши переменные
                payloadJson = JsonSerializer.Serialize(eventEnvelope);
                eventType = nameof(MoneyDebitedEvent);
                eventId = eventEnvelope.EventId;
                occurredAt = eventEnvelope.OccurredAt;
            }

            // 5. Создаем OutboxMessage ОДИН РАЗ, используя подготовленные данные
            var outboxMessage = new OutboxMessage
            {
                Id = eventId,
                Type = eventType,
                Payload = payloadJson,
                OccurredAt = occurredAt,
                CorrelationId = correlationId
            };
            outboxMessageRepository.Add(outboxMessage);

            // Явно добавляем новую транзакцию через ее собственный репозиторий
            await transactionRepository.AddAsync(newTransaction, cancellationToken);
            await accountRepository.UpdateAsync(account, cancellationToken);
            // Фиксируем ОБА изменения (баланс счета и новая транзакция) в ОДНОЙ транзакции БД
            await unitOfWork.SaveChangesAsync(cancellationToken);

            // ФИКСИРУЕМ ТРАНЗАКЦИЮ
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            var res = mapper.Map<TransactionDto>(newTransaction);
            return MbResult<TransactionDto>.Success(res);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken); // Откатываем явную транзакцию
            logger.LogWarning(ex, "Конфликт параллельного доступа при регистрации транзакции для счёта {AccountId}",
                request.AccountId);
            return MbResult<TransactionDto>.Failure(MbError.Custom("Account.Conflict",
                "Не удалось выполнить операцию, данные счёта были изменены. Попробуйте снова."));
        }
        catch (Exception ex) // Лучше ловить более конкретный DbUpdateException
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken); // Откатываем явную транзакцию
            logger.LogError(ex, "Ошибка базы данных при регистрации транзакции для счёта {AccountId}",
                request.AccountId);
            return MbResult<TransactionDto>.Failure(MbError.Custom("Database.DbError",
                "Произошла ошибка при сохранении данных."));
        }
    }
}