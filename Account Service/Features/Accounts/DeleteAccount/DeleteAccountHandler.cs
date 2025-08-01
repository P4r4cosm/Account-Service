using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Exceptions;
using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

public class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, Unit>
{
    private readonly IAccountRepository _accountRepository;

    public DeleteAccountHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }
    public async Task<Unit> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId,cancellationToken);
        if (account == null) throw new NotFoundException("Account not found");
        await _accountRepository.DeleteAsync(account,cancellationToken);
        return Unit.Value;
    }
}