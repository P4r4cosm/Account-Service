using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Features.Accounts.UpdateAccount;

public class UpdateAccountHandler(
    IAccountRepository accountRepository,
    IUnitOfWork unitOfWork,
    ILogger<UpdateAccountHandler> logger,
    IClientVerificationService clientVerificationService)
    : IRequestHandler<UpdateAccountCommand, MbResult>
{
    public async Task<MbResult> Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return MbResult.Failure(MbError.Custom("Account.NotFound", $"Счёт {request.AccountId} не найден."));
        }
        
        // Проверяем существование нового владельца
        if (!await clientVerificationService.ClientExistsAsync(request.OwnerId, cancellationToken))
        {
            return MbResult.Failure(MbError.Custom("Client.NotFound", $"Клиент {request.OwnerId} не найден."));
        }
        
        // Проверяем бизнес-правило: можно ли установить ставку на этот тип счёта
        if (request.InterestRate.HasValue && account.AccountType is AccountType.Checking)
        {
            return MbResult.Failure(MbError.Custom("Account.Update.Forbidden", "Установка процентной ставки невозможна для текущих счетов."));
        }
        
        // Проверяем бизнес-правило: дата закрытия
        if (request.CloseDate.HasValue && request.CloseDate.Value < account.OpenedDate)
        {
            return MbResult.Failure(MbError.Custom("Account.Validation", "Дата закрытия не может быть раньше даты открытия."));
        }

        account.OwnerId = request.OwnerId;
        account.InterestRate = request.InterestRate; // Если в запросе придет null, поле обнулится.
        account.CloseDate = request.CloseDate;     // Если в запросе придет null, дата закрытия сотрется.
        
        await accountRepository.UpdateAsync(account, cancellationToken);
        
        try
        {
            // Фиксируем все изменения в базе данных
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "Конфликт параллельного доступа при обновлении счёта {AccountId}", request.AccountId);
            return MbResult.Failure(MbError.Custom("Account.Conflict", "Не удалось обновить счёт, так как его данные были изменены. Пожалуйста, обновите информацию и попробуйте снова."));
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Ошибка базы данных при обновлении счёта {AccountId}", request.AccountId);
            return MbResult.Failure(MbError.Custom("Database.Error", "Произошла ошибка при сохранении изменений в базу данных."));
        }
        return MbResult.Success();
    }
}