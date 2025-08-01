using AccountService.Domain.Exceptions;
using AccountService.Infrastructure.Persistence;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Transactions.RegisterTransaction;

public class RegisterTransactionHandler : IRequestHandler<RegisterTransactionCommand, TransactionDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;

    public RegisterTransactionHandler(IAccountRepository accountRepository, IMapper mapper)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
    }

    public async Task<TransactionDto> Handle(RegisterTransactionCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException($"Счёт {request.AccountId} не найден.");
        }

        if (account.CloseDate.HasValue)
        {
            throw new OperationNotAllowedException("Операции по закрытому счёту невозможны.");
        }
        
        var newTransaction = _mapper.Map<Transaction>(request);
        
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
        account.Balance += (transactionType == TransactionType.Credit) ? request.Amount : -request.Amount;
    
        account.Transactions.Add(newTransaction);
        await _accountRepository.UpdateAsync(account, cancellationToken);

        return _mapper.Map<TransactionDto>(newTransaction);
    }
}