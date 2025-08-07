
using System.Diagnostics.CodeAnalysis;
using AccountService.Infrastructure.Verification;
using AccountService.Shared.Validation;
using FluentValidation;

namespace AccountService.Features.Accounts.CreateAccount;

//Resharper решил, что валидаторы не используются
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class CreateAccountValidator: AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator(ICurrencyService currencyService) 
    {
        RuleFor(x => x.OwnerId).NotEmpty().WithMessage("ID клиента не может быть пустым.");
        
        //проверка валюты
        RuleFor(x => x.Currency)
            .MustBeValidCurrency(currencyService)
            .When(x => !string.IsNullOrEmpty(x.Currency));
        
        // проверка счёта
        RuleFor(x => x.AccountType)
            .MustBeValidAccountType()
            .When(x => !string.IsNullOrEmpty(x.AccountType));

        // Процентная ставка обязательна только для вкладов или кредитов
        RuleFor(x => x.InterestRate)
            .NotNull()
            .When(x => x.AccountType is nameof(AccountType.Deposit) or nameof(AccountType.Credit), ApplyConditionTo.CurrentValidator)
            .WithMessage("Процентная ставка обязательна для вкладов и кредитов.");
    }
}