using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

public record CreateAccountCommand(
    Guid OwnerId,
    string AccountType, // Используем наш enum
    string Currency,
    decimal? InterestRate) : IRequest<AccountDto>;