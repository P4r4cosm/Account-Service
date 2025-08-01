using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Exceptions;
using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(
    IAccountRepository accountRepository,
    IClientVerificationService clientVerificationService)
    : IRequestHandler<UpdateAccountCommand, Unit>
{
    public async Task<Unit> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException($"Счёт {request.AccountId} не найден.");
        }
        
        // Проверяем существование нового владельца
        if (!await clientVerificationService.ClientExistsAsync(request.OwnerId, cancellationToken))
        {
            throw new NotFoundException($"Клиент {request.OwnerId} не найден.");
        }
        
        // Проверяем бизнес-правило: можно ли установить ставку на этот тип счёта
        if (request.InterestRate.HasValue && account.AccountType is AccountType.Checking)
        {
            throw new OperationNotAllowedException("Установка процентной ставки невозможна для текущих счетов.");
        }
        
        // Проверяем бизнес-правило: дата закрытия
        if (request.CloseDate.HasValue && request.CloseDate.Value < account.OpenedDate)
        {
            throw new OperationNotAllowedException("Дата закрытия не может быть раньше даты открытия.");
        }

        account.OwnerId = request.OwnerId;
        account.InterestRate = request.InterestRate; // Если в запросе придет null, поле обнулится.
        account.CloseDate = request.CloseDate;     // Если в запросе придет null, дата закрытия сотрется.
        
        await accountRepository.UpdateAsync(account, cancellationToken);
        return Unit.Value;
    }
}