using AutoMapper;
using BankAccounts.Infrastructure.Persistence;
using MediatR;

namespace BankAccounts.Features.Accounts.GetAccountById;

public class GetAccountByIdHendler : IRequestHandler<GetAccountByIdQuery, AccountDto?>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;

    public GetAccountByIdHendler(IAccountRepository accountRepository, IMapper mapper)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
    }

    public async Task<AccountDto?> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        
        return _mapper.Map<AccountDto?>(account);
        
    }
}