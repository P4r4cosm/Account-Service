using AccountService.Domain.Exceptions;
using AccountService.Infrastructure.Persistence;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountById.GetAccountField;

public class GetAccountFieldHandler : IRequestHandler<GetAccountFieldQuery, object?>
{
    private readonly IAccountRepository _accountRepository;

    public GetAccountFieldHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<object?> Handle(GetAccountFieldQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
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
            "closeddate" => account.ClosedDate,
            _ => null // Если поле не найдено или не разрешено к просмотру, возвращаем null
        };
    }
}