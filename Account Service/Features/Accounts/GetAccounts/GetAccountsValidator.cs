using AccountService.Core.Validation;
using AccountService.Infrastructure.Verification;
using FluentValidation;

namespace AccountService.Features.Accounts.GetAccounts;

public class GetAccountsValidator : AbstractValidator<GetAccountsQuery>
{

    private readonly ICurrencyService _currencyService;
    // макс. размер страницы
    private const int MaxPageSize = 100;
    
    private readonly IReadOnlyCollection<string> _validAccountTypes =
        Enum.GetNames(typeof(AccountType)).ToList().AsReadOnly();
    public GetAccountsValidator(ICurrencyService currencyService)
    {
        _currencyService = currencyService;
        
        RuleFor(x => x.AccountType)
            .MustBeValidAccountType()
            .When(x => !string.IsNullOrEmpty(x.AccountType));

        RuleFor(x => x.Currency)
            .MustBeValidCurrency(_currencyService)
            .When(x => !string.IsNullOrEmpty(x.Currency));
        
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Номер страницы должен быть не меньше 1.");

        RuleFor(x => x.PageSize)
            .GreaterThanOrEqualTo(1).WithMessage("Размер страницы должен быть не меньше 1.")
            .LessThanOrEqualTo(MaxPageSize).WithMessage($"Размер страницы не может превышать {MaxPageSize}.");
    }
}