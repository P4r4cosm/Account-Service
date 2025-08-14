using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AutoMapper;
using MediatR;

namespace AccountService.Features.Accounts.GetAccounts;

public class GetAccountsHandler(IAccountRepository accountRepository, IMapper mapper)
    : IRequestHandler<GetAccountsQuery, MbResult<PagedResult<AccountDto>>>
{
    public async Task<MbResult<PagedResult<AccountDto>>> Handle(GetAccountsQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Делегируем всю сложную работу репозиторию.
        // Передаем весь объект 'request', который содержит все фильтры.
        var pagedAccounts = await accountRepository.GetPagedAccountsAsync(request, cancellationToken);
        
        // 2. Маппим только ту страницу данных, которую получили от репозитория.
        var accountsDto = mapper.Map<IEnumerable<AccountDto>>(pagedAccounts.Items);

        // 3. Создаем финальный PagedResult для DTO.
        var result = new PagedResult<AccountDto>(
            accountsDto,
            pagedAccounts.TotalCount,
            pagedAccounts.PageNumber,
            pagedAccounts.PageSize
        );

        return MbResult<PagedResult<AccountDto>>.Success(result);
    }
}