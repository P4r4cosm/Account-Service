using MediatR;

namespace AccountService.Features.Accounts.GetAccounts;

public record GetAllAccountsQuery() : IRequest<List<AccountDto>>;
