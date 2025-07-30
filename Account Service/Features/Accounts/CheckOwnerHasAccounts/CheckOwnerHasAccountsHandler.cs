using AccountService.Infrastructure.Persistence;
using MediatR;

namespace AccountService.Features.Accounts.CheckOwnerHasAccounts;

public class CheckOwnerHasAccountsHandler : IRequestHandler<CheckOwnerHasAccountsQuery, bool>
{
    private readonly IAccountRepository _accountRepository;

    public CheckOwnerHasAccountsHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<bool> Handle(CheckOwnerHasAccountsQuery request, CancellationToken cancellationToken)
    {
        return await _accountRepository.OwnerHasAccountsAsync(request.OwnerId, cancellationToken);
    }
}