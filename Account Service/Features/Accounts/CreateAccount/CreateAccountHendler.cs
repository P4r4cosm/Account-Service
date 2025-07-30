using AccountService.Domain.Exceptions;
using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Verification;
using AutoMapper;
using MediatR;
namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHendler: IRequestHandler<CreateAccountCommand, AccountDto>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IClientVerificationService _clientVerificationService;
    private readonly IMapper _mapper;

    public CreateAccountHendler(IAccountRepository accountRepository, IMapper mapper, IClientVerificationService clientVerificationService)
    {
        _accountRepository = accountRepository;
        _mapper = mapper;
        _clientVerificationService = clientVerificationService;
    }

    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var clientExists = await _clientVerificationService.ClientExistsAsync(request.OwnerId);
        if (!clientExists)
        {
            // Бросаем исключение, которое будет поймано глобальным фильтром и превращено в 400/404.
            throw new NotFoundException($"Клиент с ID '{request.OwnerId}' не найден.");
        }
 
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