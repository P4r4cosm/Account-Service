using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Accounts.PatchAccount;

public class PatchAccountHandler(
    IAccountRepository accountRepository,
    IClientVerificationService clientVerificationService)
    : IRequestHandler<PatchAccountCommand, MbResult>
{
    public async Task<MbResult> Handle(PatchAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return MbResult.Failure(
                MbError.Custom("Account.NotFound", $"Счёт {request.AccountId} не найден."));
        }

        var hasChanges = false;

        // 1. Обновляем OwnerId, если он был передан
        if (request.OwnerId.HasValue && request.OwnerId.Value != account.OwnerId)
        {
            if (!await clientVerificationService.ClientExistsAsync(request.OwnerId.Value, cancellationToken))
            {
                return MbResult.Failure(
                    MbError.Custom("Owner.NotFound", $"Клиент {request.OwnerId.Value} не найден"));
            }
            account.OwnerId = request.OwnerId.Value;
            hasChanges = true;
        }

        // 2. Обновляем InterestRate, если он был передан
        if (request.InterestRate.HasValue && request.InterestRate.Value != account.InterestRate)
        {
            if (account.AccountType is AccountType.Checking)
            {
                // Ошибка бизнес-логики - 400 Bad Request
                return MbResult.Failure(MbError.Custom("Account.Update.Forbidden", "Установка процентной ставки недоступна для текущих счетов."));
            }
            account.InterestRate = request.InterestRate.Value;
            hasChanges = true;
        }
        
        // 3. Обновляем дату закрытия, если она передана
        if (request.CloseDate.HasValue)
        {
            if (request.CloseDate.Value < account.OpenedDate)
            {
                // Ошибка валидации данных - 400 Bad Request
                return MbResult.Failure(MbError.Custom("Account.Validation", "Дата закрытия не может быть раньше даты открытия."));
            }
            account.CloseDate = request.CloseDate;
            hasChanges = true;
        }

        // Сохраняем изменения, только если они были
        if (hasChanges)
        {
            await accountRepository.UpdateAsync(account, cancellationToken);
        }

        return MbResult.Success();
    }
}