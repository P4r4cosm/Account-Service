using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Exceptions;
using FluentValidation;
using MediatR;

namespace AccountService.Features.Accounts.PatchAccount;

public class PatchAccountHandler : IRequestHandler<PatchAccountCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IClientVerificationService _clientVerificationService;

    public PatchAccountHandler(IAccountRepository accountRepository, IClientVerificationService clientVerificationService)
    {
        _accountRepository = accountRepository;
        _clientVerificationService = clientVerificationService;
    }

    public async Task<Unit> Handle(PatchAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException($"Счёт {request.AccountId} не найден.");
        }

        bool hasChanges = false;

        // 1. Обновляем OwnerId, если он был передан
        if (request.OwnerId.HasValue && request.OwnerId.Value != account.OwnerId)
        {
            if (!await _clientVerificationService.ClientExistsAsync(request.OwnerId.Value, cancellationToken))
            {
                throw new NotFoundException($"Клиент {request.OwnerId.Value} не найден.");
            }
            account.OwnerId = request.OwnerId.Value;
            hasChanges = true;
        }

        // 2. Обновляем InterestRate, если он был передан
        if (request.InterestRate.HasValue)
        {
            if (account.AccountType is AccountType.Checking)
            {
                throw new OperationNotAllowedException("Установка процентной ставки недоступна для текущих счетов.");
            }
            account.InterestRate = request.InterestRate;
            hasChanges = true;
        }
        
        // 3. Обновляем дату закрытия, если она передана
        if (request.CloseDate.HasValue)
        {
            if (request.CloseDate.Value < account.OpenedDate)
            {
                throw new ValidationException("Дата закрытия не может быть раньше даты открытия.");
            }
            account.CloseDate = request.CloseDate;
            hasChanges = true;
        }

        // Сохраняем изменения, только если они были
        if (hasChanges)
        {
            await _accountRepository.UpdateAsync(account, cancellationToken);
        }

        return Unit.Value;
    }
}