using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Exceptions;
using AutoMapper;
using MediatR;
namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IAccountRepository accountRepository,
    IMapper mapper,
    IClientVerificationService clientVerificationService)
    : IRequestHandler<CreateAccountCommand, AccountDto>
{
    public async Task<AccountDto> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var clientExists = await clientVerificationService.ClientExistsAsync(request.OwnerId, cancellationToken);
        if (!clientExists)
        {
            // Бросаем исключение, которое будет поймано глобальным фильтром и превращено в 400/404.
            throw new NotFoundException($"Клиент с ID '{request.OwnerId}' не найден.");
        }
 
        //создаём account с помощью automapper
        var account = mapper.Map<Account>(request);
        //изменяем необходимые поля
        account.Id=Guid.NewGuid();
        account.Balance=0;
        account.OpenedDate=DateTime.UtcNow;
        //сохраняем
        await accountRepository.AddAsync(account, cancellationToken);
        
        //создаём dto из account
        var resultDto=mapper.Map<AccountDto>(account);
        return resultDto;
    }
}