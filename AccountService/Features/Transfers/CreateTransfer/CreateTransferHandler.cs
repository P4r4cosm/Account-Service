using System.Data;
using AccountService.Features.Transactions;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AccountService.Features.Transfers.CreateTransfer;

public class CreateTransferHandler(
    IAccountRepository accountRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork,
    ILogger<CreateTransferHandler> logger)
    : IRequestHandler<CreateTransferCommand, MbResult>
{
    public async Task<MbResult> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return MbResult.Failure(MbError.Custom("Transfer.InvalidAmount", "Сумма перевода должна быть больше нуля."));
        }

        // Загружаем оба счёта в одной операции с блокировкой для сериализуемой изоляции
        await unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            

            var fromAccount = await accountRepository.GetByIdAsync(request.FromAccountId, cancellationToken);
            if (fromAccount is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return MbResult.Failure(MbError.Custom("Transfer.NotFound",
                    $"Счёт списания {request.FromAccountId} не найден."));
            }
            var toAccount = await accountRepository.GetByIdAsync(request.ToAccountId, cancellationToken);
            if (toAccount is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return MbResult.Failure(MbError.Custom("Transfer.NotFound",
                    $"Счёт зачисления {request.ToAccountId} не найден."));
            }

            // Проверки бизнес-логики
            if (fromAccount.Id == toAccount.Id)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return MbResult.Failure(MbError.Custom("Transfer.Validation", "Перевод на тот же счёт невозможен."));
            }

            if (fromAccount.CloseDate.HasValue)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return MbResult.Failure(MbError.Custom("Transfer.Validation",
                    $"Счёт списания {fromAccount.Id} закрыт."));
            }

            if (toAccount.CloseDate.HasValue)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return MbResult.Failure(MbError.Custom("Transfer.Validation",
                    $"Счёт зачисления {toAccount.Id} закрыт."));
            }

            if (fromAccount.Currency != toAccount.Currency)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return MbResult.Failure(MbError.Custom("Transfer.Validation",
                    "Переводы возможны только между счетами в одной валюте."));
            }

            if (fromAccount.Balance < request.Amount)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return MbResult.Failure(MbError.Custom("Transfer.Validation",
                    $"Недостаточно средств на счёте {fromAccount.Id}."));
            }

            // Формируем описания
            var timestamp = DateTime.UtcNow;
            var debitDescription = $"Перевод на счёт {toAccount.Id}." + (string.IsNullOrEmpty(request.Description) ? "" : $" {request.Description}");
            var creditDescription = $"Перевод со счёта {fromAccount.Id}." + (string.IsNullOrEmpty(request.Description) ? "" : $" {request.Description}");

            // Создаём транзакции
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

            // Обновляем балансы
            fromAccount.Balance -= request.Amount;
            toAccount.Balance += request.Amount;

            // Добавляем транзакции
            fromAccount.Transactions.Add(debitTransaction);
            toAccount.Transactions.Add(creditTransaction);

            await transactionRepository.AddAsync(debitTransaction, cancellationToken);
            await transactionRepository.AddAsync(creditTransaction, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return MbResult.Success();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            logger.LogWarning(ex, "Конфликт параллельного доступа при переводе со счёта {FromAccountId} на {ToAccountId}", 
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