using MediatR;

namespace BankAccounts.Features.Accounts.GetAccountById;

public record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountDto?>;
