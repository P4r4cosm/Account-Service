using AccountService.Domain.Exceptions;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Verification;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler: IRequestHandler<UpdateAccountCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IClientVerificationService  _clientVerificationService;
    public UpdateAccountHandler(IAccountRepository accountRepository, IClientVerificationService clientVerificationService)
    {
        _accountRepository = accountRepository;
        _clientVerificationService = clientVerificationService;
    }
    public async Task<Unit> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        // получаем изменяемый счёт
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        // проверяем существует ли он
        if (account is null)
        {
            throw new NotFoundException($"Счёт {request.AccountId} не найден.");
        }
        
        // проверяем существование нового владельца
        if (!await _clientVerificationService.ClientExistsAsync(request.OwnerId))
        {
            throw new NotFoundException($"Клиент {request.OwnerId} не найден.");
        }
        
        // Ставку можно менять только у вкладов и кредитов
        if (account.AccountType is AccountType.Checking)
        {
            throw new OperationNotAllowedException("Изменение процентной ставки доступно только для вкладов и кредитов.");
        }
        
        account.OwnerId = request.OwnerId;
        account.InterestRate = request.InterestRate;
        
        await _accountRepository.UpdateAsync(account, cancellationToken);
        return Unit.Value;
    }
}