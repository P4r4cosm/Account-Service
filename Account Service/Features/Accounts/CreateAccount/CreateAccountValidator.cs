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


        RuleFor(x => x.AccountType)
            .NotEmpty().WithMessage("Тип счёта не может быть пустым.")
            .Must(BeAValidAccountType)
            .WithMessage("Указан некорректный тип счёта. Допустимые значения: Checking, Deposit, Credit.");

        // Процентная ставка обязательна только для вкладов или кредитов
        RuleFor(x => x.InterestRate)
            .NotNull()
            .When(x => x.AccountType == nameof(AccountType.Deposit) || x.AccountType == nameof(AccountType.Credit), ApplyConditionTo.CurrentValidator)
            .WithMessage("Процентная ставка обязательна для вкладов и кредитов.");
    }
    
    /// <summary>
    /// Метод для асинхронной валидации через наш сервис.
    /// </summary>
    private async Task<bool> BeASupportedCurrency(string currencyCode, CancellationToken cancellationToken)
    {
        return await _currencyService.IsSupportedAsync(currencyCode, cancellationToken);
    }
    
    // Вспомогательный метод для проверки, является ли строка валидным значением enum
    private bool BeAValidAccountType(string accountType)
    {
        // Enum.TryParse вернет true, если строка (с игнорированием регистра)
        // может быть успешно преобразована в значение AccountType.
        return Enum.TryParse<AccountType>(accountType, ignoreCase: true, out _);
    }
}