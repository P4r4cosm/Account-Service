using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Accounts.CheckOwnerHasAccounts;

public record CheckOwnerHasAccountsQuery(Guid OwnerId) : IRequest<MbResult<bool>>;
