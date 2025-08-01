
using AccountService.Features.Transactions;
using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Exceptions;
using MediatR;

namespace AccountService.Features.Transfers.CreateTransfer;

public class CreateTransferHandler(IAccountRepository accountRepository) : IRequestHandler<CreateTransferCommand, Unit>
{
    // В будущем здесь может понадобиться IUnitOfWork для обеспечения транзакционности БД

    public async Task<Unit> Handle(CreateTransferCommand request, CancellationToken cancellationToken)
    {
        // 1. Получаем оба счёта
        var fromAccount = await accountRepository.GetByIdAsync(request.FromAccountId, cancellationToken);
        if (fromAccount is null)
            throw new NotFoundException($"Счёт списания {request.FromAccountId} не найден.");

        var toAccount = await accountRepository.GetByIdAsync(request.ToAccountId, cancellationToken);
        if (toAccount is null)
            throw new NotFoundException($"Счёт зачисления {request.ToAccountId} не найден.");

        // 2. Выполняем бизнес-проверки
        if (fromAccount.CloseDate.HasValue)
            throw new OperationNotAllowedException($"Счёт списания {fromAccount.Id} закрыт.");

        if (toAccount.CloseDate.HasValue)
            throw new OperationNotAllowedException($"Счёт зачисления {toAccount.Id} закрыт.");

        if (fromAccount.Currency != toAccount.Currency)
            throw new OperationNotAllowedException("Переводы возможны только между счетами в одной валюте.");

        if (fromAccount.Balance < request.Amount)
            throw new OperationNotAllowedException("Недостаточно средств на счёте списания.");

        // 3. Создаём две транзакции
        var debitDescription = $"Перевод на счёт {toAccount.Id}.";
        if (!string.IsNullOrEmpty(request.Description)) debitDescription += $" {request.Description}";

        var creditDescription = $"Перевод со счёта {fromAccount.Id}.";
        if (!string.IsNullOrEmpty(request.Description)) creditDescription += $" {request.Description}";

        var debitTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = fromAccount.Id,
            CounterpartyAccountId = toAccount.Id, 
            Amount = request.Amount,
            Currency = fromAccount.Currency,
            Type = TransactionType.Debit,
            Description = debitDescription,
            Timestamp = DateTime.UtcNow
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
            Timestamp = DateTime.UtcNow
        };

        // 4. Обновляем балансы и добавляем транзакции
        fromAccount.Balance -= request.Amount;
        fromAccount.Transactions.Add(debitTransaction);

        toAccount.Balance += request.Amount;
        toAccount.Transactions.Add(creditTransaction);

        // 5. Сохраняем изменения
        await accountRepository.UpdateAsync(fromAccount, cancellationToken);
        await accountRepository.UpdateAsync(toAccount, cancellationToken);

        return Unit.Value;
    }
}