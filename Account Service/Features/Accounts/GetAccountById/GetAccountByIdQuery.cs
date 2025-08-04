using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountById;

public class GetAccountByIdQuery : IRequest<MbResult<AccountDto>>
{
   public Guid AccountId { get; init; }
}