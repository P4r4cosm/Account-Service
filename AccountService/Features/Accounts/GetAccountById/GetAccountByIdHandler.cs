using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountById;

public class GetAccountByIdHandler(IAccountRepository accountRepository, IMapper mapper)
    : IRequestHandler<GetAccountByIdQuery, MbResult<AccountDto>>
{
    // 2. Меняем тип возвращаемого значения и здесь
    public async Task<MbResult<AccountDto>> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(request.AccountId, cancellationToken);
        
        if (account is null)
        {
            return MbResult<AccountDto>.Failure(
                MbError.Custom("Account.NotFound", $"Счёт {request.AccountId} не найден."));
        }
        
        // 3. Мапим в не-nullable DTOr
        var accountDto = mapper.Map<AccountDto>(account);

        // 4. Возвращаем успешный результат
        return MbResult<AccountDto>.Success(accountDto);
    }
}