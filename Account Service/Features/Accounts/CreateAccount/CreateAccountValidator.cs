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
            .NotEmpty()
            .Length(3)
            .MustAsync(BeASupportedCurrency)
            .WithMessage("Указанная валюта не поддерживается.");
        
        
        RuleFor(x => x.AccountType).IsInEnum();

        // Процентная ставка обязательна только для вкладов или кредитов
        RuleFor(x => x.InterestRate)
            .NotNull()
            .When(x => x.AccountType == AccountType.Deposit || x.AccountType == AccountType.Credit)
            .WithMessage("Процентная ставка обязательна для вкладов и кредитов.");
    }
    
    /// <summary>
    /// Метод для асинхронной валидации через наш сервис.
    /// </summary>
    private async Task<bool> BeASupportedCurrency(string currencyCode, CancellationToken cancellationToken)
    {
        return await _currencyService.IsSupportedAsync(currencyCode, cancellationToken);
    }
}