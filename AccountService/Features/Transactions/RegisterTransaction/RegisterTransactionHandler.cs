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
            DomainEvent domainEvent = newTransaction.Type == TransactionType.Debit
                ? new MoneyDebitedEvent(correlationId, causationId)
                {
                    AccountId = newTransaction.AccountId,
                    Currency = newTransaction.Currency,
                    Amount = newTransaction.Amount,
                    OperationId = newTransaction.Id,
                    Reason = newTransaction.Description
                }
                : new MoneyCreditedEvent(correlationId, causationId)
                {
                    AccountId = newTransaction.AccountId,
                    Currency = newTransaction.Currency,
                    Amount = newTransaction.Amount,
                    OperationId = newTransaction.Id
                };

            var outboxMessage = new OutboxMessage
            {
                Id = domainEvent.EventId,
                // Используем GetType().Name, чтобы получить имя конкретного класса (MoneyCreditedEvent и т.д.)
                Type = domainEvent.GetType().Name,
                Payload = JsonSerializer.Serialize(domainEvent,
                    domainEvent.GetType()), // Важно передать тип для полиморфной сериализации
                OccurredAt = domainEvent.OccurredAt,
                CorrelationId = domainEvent.Meta.CorrelationId
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