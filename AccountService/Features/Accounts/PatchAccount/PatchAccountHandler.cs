using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Features.Accounts.PatchAccount;

public class PatchAccountHandler(
    IAccountRepository accountRepository,
    IClientVerificationService clientVerificationService,
    IUnitOfWork unitOfWork,
    ILogger<PatchAccountHandler> logger)
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
                return MbResult.Failure(MbError.Custom("Account.Update.Forbidden",
                    "Установка процентной ставки недоступна для текущих счетов."));
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
                return MbResult.Failure(MbError.Custom("Account.Validation",
                    "Дата закрытия не может быть раньше даты открытия."));
            }

            account.CloseDate = request.CloseDate;
            hasChanges = true;
        }

        // Сохраняем изменения, только если они были
        if (!hasChanges) return MbResult.Success();
        await accountRepository.UpdateAsync(account, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Конфликт параллельного доступа при обновлении счёта {AccountId}",
                request.AccountId);
            return MbResult.Failure(MbError.Custom("Account.Conflict",
                "Не удалось обновить счёт, так как его данные были изменены. Пожалуйста, обновите информацию и попробуйте снова."));
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Ошибка базы данных при обновлении счёта {AccountId}", request.AccountId);
            return MbResult.Failure(MbError.Custom("Database.Error",
                "Произошла ошибка при сохранении изменений в базу данных."));
        }
        return MbResult.Success();
    }
}