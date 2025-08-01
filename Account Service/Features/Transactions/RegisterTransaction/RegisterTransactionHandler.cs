using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Exceptions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Transactions.RegisterTransaction;

public class RegisterTransactionHandler(IAccountRepository accountRepository, IMapper mapper)
    : IRequestHandler<RegisterTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException($"Счёт {request.AccountId} не найден.");
        }

        if (account.CloseDate.HasValue)
        {
            throw new OperationNotAllowedException("Операции по закрытому счёту невозможны.");
        }
        
        var newTransaction = mapper.Map<Transaction>(request);
        
        newTransaction.Id = Guid.NewGuid();
        newTransaction.AccountId = account.Id;
        newTransaction.Currency = account.Currency; // Берем валюту со счёта
        newTransaction.Timestamp = DateTime.UtcNow;
    
        // Обновляем баланс
        var transactionType = newTransaction.Type;
        
        if (transactionType == TransactionType.Debit && account.Balance < request.Amount)
        {
            throw new OperationNotAllowedException("Недостаточно средств на счёте для списания.");
        }
        account.Balance += transactionType == TransactionType.Credit ? request.Amount : -request.Amount;
    
        account.Transactions.Add(newTransaction);
        await accountRepository.UpdateAsync(account, cancellationToken);

        return mapper.Map<TransactionDto>(newTransaction);
    }
}