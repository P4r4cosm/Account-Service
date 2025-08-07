using AccountService.Infrastructure.Persistence;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Domain;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountHandler(
    IAccountRepository accountRepository,
    IMapper mapper,
    IClientVerificationService clientVerificationService)
    : IRequestHandler<CreateAccountCommand, MbResult<AccountDto>>
{
    public async Task<MbResult<AccountDto>> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var clientExists = await clientVerificationService.ClientExistsAsync(request.OwnerId, cancellationToken);
        if (!clientExists)
        {
            return MbResult<AccountDto>.Failure(MbError.Custom("Client.NotFound", $"Клиент с ID '{request.OwnerId}' не найден."));
        }

        var account = mapper.Map<Account>(request);
        account.Id = Guid.NewGuid();
        account.Balance = 0;
        account.OpenedDate = DateTime.UtcNow;
        
        await accountRepository.AddAsync(account, cancellationToken);
        
        // Сразу мапим созданную сущность в DTO
        var accountDto = mapper.Map<AccountDto>(account);
        
        // Возвращаем DTO в успешном результате
        return MbResult<AccountDto>.Success(accountDto);
    }
}