using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Accounts.GetAccountById.GetAccountField;

public record GetAccountFieldQuery(Guid AccountId, string FieldName) : IRequest<MbResult<object?>>;
