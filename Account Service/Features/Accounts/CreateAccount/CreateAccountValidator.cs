using AccountService.Core.Validation;
using AccountService.Infrastructure.Verification;
using FluentValidation;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountValidator: AbstractValidator<CreateAccountCommand>
{
    private readonly ICurrencyService _currencyService;
    public CreateAccountValidator(ICurrencyService currencyService) 
    {
        _currencyService = currencyService;
        RuleFor(x => x.OwnerId).NotEmpty();
        
        //проверка валюты
        RuleFor(x => x.Currency)
            .MustBeValidCurrency(_currencyService)
            .When(x => !string.IsNullOrEmpty(x.Currency));
        
        // проверка счёта
        RuleFor(x => x.AccountType)
            .MustBeValidAccountType()
            .When(x => !string.IsNullOrEmpty(x.AccountType));

        // Процентная ставка обязательна только для вкладов или кредитов
        RuleFor(x => x.InterestRate)
            .NotNull()
            .When(x => x.AccountType == nameof(AccountType.Deposit) || x.AccountType == nameof(AccountType.Credit), ApplyConditionTo.CurrentValidator)
            .WithMessage("Процентная ставка обязательна для вкладов и кредитов.");
    }
}