using System.Data;
using System.Text.Json;
using AccountService.Features.Accounts;
using AccountService.Features.Transactions;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using AccountService.Shared.Providers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AccountService.Features.Transfers.CreateTransfer;

public class CreateTransferHandler(
    IAccountRepository accountRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork,
    IOutboxMessageRepository outboxMessageRepository,
    ICorrelationIdProvider correlationIdProvider,
    ILogger<CreateTransferHandler> logger)
    : IRequestHandler<CreateTransferCommand, MbResult>
{
    public async Task<MbResult> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return MbResult.Failure(MbError.Custom("Transfer.InvalidAmount",
                "Сумма перевода должна быть больше нуля."));
        }

        // Загружаем оба счёта в одной операции с блокировкой для сериализуемой изоляции
        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var fromAccount = await accountRepository.GetByIdAsync(request.FromAccountId, cancellationToken);
            var toAccount = await accountRepository.GetByIdAsync(request.ToAccountId, cancellationToken);
            var validationResult = await ValidateTransfer(request, fromAccount, toAccount);
            if (validationResult.IsFailure)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return validationResult;
            }

            // После успешной валидации продолжаем операцию
            var (debitTransaction, creditTransaction) = CreateTransactions(request, fromAccount!, toAccount!);
            UpdateAccountBalances(fromAccount!, toAccount!, request.Amount);

            fromAccount!.Transactions.Add(debitTransaction);
            toAccount!.Transactions.Add(creditTransaction);

            await transactionRepository.AddAsync(debitTransaction, cancellationToken);
            await transactionRepository.AddAsync(creditTransaction, cancellationToken);

            var correlationId = correlationIdProvider.GetCorrelationId();
            // В качестве ID причины используем ID самой команды на перевод
            var causationId = request.CommandId; 

            // Генерируем уникальный ID для самой операции перевода.
            var transferId = Guid.NewGuid();

            var transferCompletedEvent = new TransferCompletedEvent
            {
                SourceAccountId = fromAccount.Id,
                DestinationAccountId = toAccount.Id,
                Amount = request.Amount,
                Currency = fromAccount.Currency,
                TransferId = transferId
            };
            var eventEnvelope = new EventEnvelope<TransferCompletedEvent>(transferCompletedEvent, correlationId, causationId);

            
            // 3. Создаем и добавляем сообщение в Outbox
            var outboxMessage = new OutboxMessage
            {
                Id = eventEnvelope.EventId,
                Type = nameof(TransferCompletedEvent),
                Payload = JsonSerializer.Serialize(eventEnvelope),
                OccurredAt = eventEnvelope.OccurredAt,
                CorrelationId = eventEnvelope.Meta.CorrelationId
            };
            outboxMessageRepository.Add(outboxMessage);
            
            
            
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return MbResult.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogWarning(ex,
                "Конфликт параллельного доступа при переводе со счёта {FromAccountId} на {ToAccountId}",
                request.FromAccountId, request.ToAccountId);
            return MbResult.Failure(MbError.Custom(
                "Transfer.Conflict",
                "Данные одного из счетов были изменены параллельно. Повторите операцию."));
        }
        catch (Exception ex)
        {
            if (IsSerializationFailure(ex))
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                logger.LogWarning(ex, "Конфликт сериализации при переводе со счёта {FromAccountId} на {ToAccountId}",
                    request.FromAccountId, request.ToAccountId);
                return MbResult.Failure(MbError.Custom(
                    "Transfer.Conflict",
                    "Данные одного из счетов были изменены параллельно. Повторите операцию."));
            }

            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogError(ex, "Ошибка при выполнении перевода со счёта {FromAccountId} на {ToAccountId}",
                request.FromAccountId, request.ToAccountId);
            return MbResult.Failure(MbError.Custom("Transfer.Error", ex.Message));
        }
    }

    private static (Transaction debit, Transaction credit) CreateTransactions(
        CreateTransferCommand request,
        Account fromAccount,
        Account toAccount)
    {
        var timestamp = DateTime.UtcNow;
        var debitDescription = $"Перевод на счёт {toAccount.Id}." +
                               (string.IsNullOrEmpty(request.Description) ? "" : $" {request.Description}");
        var creditDescription = $"Перевод со счёта {fromAccount.Id}." +
                                (string.IsNullOrEmpty(request.Description) ? "" : $" {request.Description}");

        var debitTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = fromAccount.Id,
            CounterpartyAccountId = toAccount.Id,
            Amount = request.Amount,
            Currency = fromAccount.Currency,
            Type = TransactionType.Debit,
            Description = debitDescription,
            Timestamp = timestamp
        };

        var creditTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = toAccount.Id,
            CounterpartyAccountId = fromAccount.Id,
            Amount = request.Amount,
            Currency = toAccount.Currency,
            Type = TransactionType.Credit,
            Description = creditDescription,
            Timestamp = timestamp
        };

        return (debitTransaction, creditTransaction);
    }

    private static void UpdateAccountBalances(Account fromAccount, Account toAccount, decimal amount)
    {
        fromAccount.Balance -= amount;
        toAccount.Balance += amount;
    }

    private static Task<MbResult> ValidateTransfer(
        CreateTransferCommand request,
        Account? fromAccount,
        Account? toAccount)
    {
        if (fromAccount is null)
        {
            return Task.FromResult(MbResult.Failure(MbError.Custom("Transfer.NotFound",
                $"Счёт списания {request.FromAccountId} не найден.")));
        }

        if (toAccount is null)
        {
            return Task.FromResult(MbResult.Failure(MbError.Custom("Transfer.NotFound",
                $"Счёт зачисления {request.ToAccountId} не найден.")));
        }

        if (fromAccount.Id == toAccount.Id)
        {
            return Task.FromResult(MbResult.Failure(MbError.Custom("Transfer.Validation", "Перевод на тот же счёт невозможен.")));
        }

        if (fromAccount.CloseDate.HasValue)
        {
            return Task.FromResult(MbResult.Failure(MbError.Custom("Transfer.Validation",
                $"Счёт списания {fromAccount.Id} закрыт.")));
        }

        if (toAccount.CloseDate.HasValue)
        {
            return Task.FromResult(MbResult.Failure(MbError.Custom("Transfer.Validation",
                $"Счёт зачисления {toAccount.Id} закрыт.")));
        }

        if (fromAccount.Currency != toAccount.Currency)
        {
            return Task.FromResult(MbResult.Failure(MbError.Custom("Transfer.Validation",
                "Переводы возможны только между счетами в одной валюте.")));
        }

        if (fromAccount.Balance < request.Amount)
        {
            return Task.FromResult(MbResult.Failure(MbError.Custom("Transfer.Validation",
                $"Недостаточно средств на счёте {fromAccount.Id}.")));
        }

        return Task.FromResult(MbResult.Success());
    }


    private static bool IsSerializationFailure(Exception? ex)
    {
        while (ex != null)
        {
            if (ex is NpgsqlException { SqlState: "40001" })
            {
                return true;
            }

            ex = ex.InnerException;
        }

        return false;
    }
}