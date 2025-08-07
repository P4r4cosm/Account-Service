using AccountService.Features.Transactions;
using AccountService.Infrastructure.Persistence; // Предполагается, что здесь IUnitOfWork
using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Transfers.CreateTransfer;

public class CreateTransferHandler(
    IAccountRepository accountRepository)
    : IRequestHandler<CreateTransferCommand, MbResult> 
{
    public async Task<MbResult> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        // Получаем оба счёта
        var fromAccount = await accountRepository.GetByIdAsync(request.FromAccountId, cancellationToken);
        if (fromAccount is null)
        {
            return MbResult.Failure(MbError.Custom("Transfer.FromAccountNotFound", $"Счёт списания {request.FromAccountId} не найден."));
        }

        var toAccount = await accountRepository.GetByIdAsync(request.ToAccountId, cancellationToken);
        if (toAccount is null)
        {
            return MbResult.Failure(MbError.Custom("Transfer.ToAccountNotFound", $"Счёт зачисления {request.ToAccountId} не найден."));
        }

        //  Выполняем бизнес-проверки, возвращая ошибки через MbResult
        if (fromAccount.Id == toAccount.Id)
        {
            return MbResult.Failure(MbError.Custom("Transfer.SameAccount", "Перевод на тот же самый счёт невозможен."));
        }

        if (fromAccount.CloseDate.HasValue)
        {
            return MbResult.Failure(MbError.Custom("Transfer.FromAccountClosed", $"Счёт списания {fromAccount.Id} закрыт."));
        }

        if (toAccount.CloseDate.HasValue)
        {
            return MbResult.Failure(MbError.Custom("Transfer.ToAccountClosed", $"Счёт зачисления {toAccount.Id} закрыт."));
        }

        if (fromAccount.Currency != toAccount.Currency)
        {
            return MbResult.Failure(MbError.Custom("Transfer.CurrencyMismatch", "Переводы возможны только между счетами в одной валюте."));
        }

        if (fromAccount.Balance < request.Amount)
        {
            return MbResult.Failure(MbError.Custom("Transfer.InsufficientFunds", "Недостаточно средств на счёте списания."));
        }

        // Создаём две транзакции
        var debitDescription = $"Перевод на счёт {toAccount.Id}.";
        if (!string.IsNullOrEmpty(request.Description)) debitDescription += $" {request.Description}";

        var creditDescription = $"Перевод со счёта {fromAccount.Id}.";
        if (!string.IsNullOrEmpty(request.Description)) creditDescription += $" {request.Description}";
        
        var timestamp = DateTime.UtcNow;

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

        //  Обновляем балансы и добавляем транзакции
        fromAccount.Balance -= request.Amount;
        fromAccount.Transactions.Add(debitTransaction);

        toAccount.Balance += request.Amount;
        toAccount.Transactions.Add(creditTransaction);
        await accountRepository.UpdateAsync(fromAccount, cancellationToken);
        await accountRepository.UpdateAsync(toAccount, cancellationToken);
        

        // Возвращаем успешный результат
        return MbResult.Success();
    }
}