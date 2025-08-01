
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Validation;
using FluentValidation;

namespace AccountService.Features.Accounts.GetAccounts;

public class GetAccountsValidator : AbstractValidator<GetAccountsQuery>
{
    // макс. размер страницы
    private const int MaxPageSize = 100;
    public GetAccountsValidator(ICurrencyService currencyService)
    {
        RuleFor(x => x.AccountType)
            .MustBeValidAccountType()
            .When(x => !string.IsNullOrEmpty(x.AccountType));

        RuleFor(x => x.Currency)!
            .MustBeValidCurrency(currencyService)
            .When(x => !string.IsNullOrEmpty(x.Currency));
        
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Номер страницы должен быть не меньше 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Размер страницы должен быть не меньше 1.")
            .LessThanOrEqualTo(MaxPageSize).WithMessage($"Размер страницы не может превышать {MaxPageSize}.");
    }
}