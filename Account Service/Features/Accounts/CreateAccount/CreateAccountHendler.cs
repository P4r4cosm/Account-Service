using AutoMapper;
using BankAccounts.Infrastructure.Persistence;
using MediatR;
namespace BankAccounts.Features.Accounts.CreateAccount;

public class CreateAccountHendler: IRequestHandler<CreateAccountCommand, AccountDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IMapper _mapper;

    public CreateAccountHendler(IAccountRepository accountRepository, IMapper mapper)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
    }

    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        //создаём account с помощью automapper
        var account = _mapper.Map<Account>(request);
        //изменяем необходимые поля
        account.Id=Guid.NewGuid();
        account.Balance=0;
        account.OpenedDate=DateTime.UtcNow;
        //сохраняем
        await _accountRepository.AddAsync(account, cancellationToken);
        
        //создаём dto из account
        var resultDto=_mapper.Map<AccountDto>(account);
        return resultDto;
    }
}