using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(IAccountRepository accountRepository) : IRequestHandler<DeleteAccountCommand, MbResult>
{
    public async Task<MbResult> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId,cancellationToken);
        if (account == null)
        {
            return MbResult.Failure(
                MbError.Custom("Account.NotFound", $"Счёт {request.AccountId} не найден."));
        }
        await accountRepository.DeleteAsync(account,cancellationToken);
        return MbResult.Success();
    }
}