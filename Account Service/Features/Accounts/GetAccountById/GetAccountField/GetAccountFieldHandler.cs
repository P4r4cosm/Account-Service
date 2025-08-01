using System.Diagnostics.CodeAnalysis;
using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Exceptions;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountById.GetAccountField;

public class GetAccountFieldHandler(IAccountRepository accountRepository)
    : IRequestHandler<GetAccountFieldQuery, object?>
{
    [SuppressMessage("ReSharper", "StringLiteralTypo")] //Resharper жалуется на приведённые в switch параметры к нижнему регистру (ownerId, accountType и т.д.)
    public async Task<object?> Handle(GetAccountFieldQuery request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException("Account not found");
        }

        return request.FieldName.ToLowerInvariant() switch
        {
            "ownerid" => account.OwnerId,
            "accounttype" => account.AccountType.ToString(),
            "currency" => account.Currency,
            "balance" => account.Balance,
            "interestrate" => account.InterestRate,
            "openeddate" => account.OpenedDate,
            "closeddate" => account.CloseDate,
            _ => null // Если поле не найдено или не разрешено к просмотру, возвращаем null
        };
    }
}