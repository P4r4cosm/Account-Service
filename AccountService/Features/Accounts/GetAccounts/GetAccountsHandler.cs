using AccountService.Infrastructure.Persistence;
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
        // 1. Получаем все счета из источника данных (в вашем случае - из in-memory списка).
        // Важно: на этом этапе мы получаем IEnumerable, то есть все данные уже в памяти.
        var accounts = await accountRepository.GetAllAsync(cancellationToken);

        // 2. Последовательно применяем фильтры, если они были указаны в запросе.
        // Каждый .Where() сужает набор данных.

        if (request.OwnerId.HasValue)
        {
            accounts = accounts.Where(a => a.OwnerId == request.OwnerId.Value);
        }

        if (!string.IsNullOrEmpty(request.AccountType))
        {
            // Сравнение без учета регистра для надежности
            accounts = accounts.Where(a =>
                a.AccountType.ToString().Equals(request.AccountType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(request.Currency))
        {
            accounts = accounts.Where(a => a.Currency.Equals(request.Currency, StringComparison.OrdinalIgnoreCase));
        }

        // Фильтрация по балансу
        if (request.BalanceGte.HasValue)
        {
            accounts = accounts.Where(a => a.Balance >= request.BalanceGte.Value);
        }

        if (request.BalanceLte.HasValue)
        {
            accounts = accounts.Where(a => a.Balance <= request.BalanceLte.Value);
        }

        // Фильтрация по дате открытия
        if (request.OpeningDateFrom.HasValue)
        {
            // Сравниваем только дату, без учета времени
            accounts = accounts.Where(a => a.OpenedDate.Date >= request.OpeningDateFrom.Value.Date);
        }

        if (request.OpeningDateTo.HasValue)
        {
            accounts = accounts.Where(a => a.OpenedDate.Date <= request.OpeningDateTo.Value.Date);
        }

        // 3. Считаем общее количество элементов ПОСЛЕ фильтрации.
        // Чтобы избежать двойного перебора, лучше материализовать коллекцию.
        var filteredAccounts = accounts.ToList();
        var totalCount = filteredAccounts.Count;

        // 4. Применяем пагинацию к отфильтрованному списку.
        var pagedData = filteredAccounts
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        // 5. Маппим только страницу данных в DTO.
        var resultDto = mapper.Map<IEnumerable<AccountDto>>(pagedData);

        // 6. Создаем и возвращаем PagedResult.
        var res = new PagedResult<AccountDto>(resultDto, totalCount, request.PageNumber, request.PageSize);
        return MbResult<PagedResult<AccountDto>>.Success(res);
    }
}