using AccountService.Infrastructure.Persistence;
using AccountService.Shared.Exceptions;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountById;

public class GetAccountByIdHandler(IAccountRepository accountRepository, IMapper mapper)
    : IRequestHandler<GetAccountByIdQuery, AccountDto?>
{
    public async Task<AccountDto?> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            throw new NotFoundException($"Счёт {request.AccountId} не найден.");
        }
        return mapper.Map<AccountDto?>(account);
        
    }
}