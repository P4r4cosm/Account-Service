using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Accounts.CheckOwnerHasAccounts;

public class CheckOwnerHasAccountsHandler(IAccountRepository accountRepository)
    : IRequestHandler<CheckOwnerHasAccountsQuery, MbResult<bool>>
{
    public async Task<MbResult<bool>> Handle(CheckOwnerHasAccountsQuery request, CancellationToken cancellationToken)
    {
        var res = await accountRepository.OwnerHasAccountsAsync(request.OwnerId, cancellationToken);
        return MbResult<bool>.Success(res);
    }
}