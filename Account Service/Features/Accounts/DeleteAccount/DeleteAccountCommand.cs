using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

public record DeleteAccountCommand(Guid AccountId): IRequest<MbResult>;
