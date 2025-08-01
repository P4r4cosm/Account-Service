using AccountService.Infrastructure.Persistence;
using MediatR;

namespace AccountService.Features.Accounts.CheckOwnerHasAccounts;

public class CheckOwnerHasAccountsHandler(IAccountRepository accountRepository)
    : IRequestHandler<CheckOwnerHasAccountsQuery, bool>
{
    public async Task<bool> Handle(CheckOwnerHasAccountsQuery request, CancellationToken cancellationToken)
    {
        return await accountRepository.OwnerHasAccountsAsync(request.OwnerId, cancellationToken);
    }
}