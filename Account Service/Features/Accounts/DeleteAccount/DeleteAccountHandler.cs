using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Exceptions;
using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler(IAccountRepository accountRepository) : IRequestHandler<DeleteAccountCommand, Unit>
{
    public async Task<Unit> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId,cancellationToken);
        if (account == null) throw new NotFoundException("Account not found");
        await accountRepository.DeleteAsync(account,cancellationToken);
        return Unit.Value;
    }
}