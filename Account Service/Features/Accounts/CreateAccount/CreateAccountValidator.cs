using FluentValidation;

namespace AccountService.Features.Accounts.CreateAccount;

public class CreateAccountValidator: AbstractValidator<CreateAccountCommand>
{
    public CreateAccountValidator() 
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.AccountType).IsInEnum();

        // Процентная ставка обязательна только для вкладов или кредитов
        RuleFor(x => x.InterestRate)
            .NotNull()
            .When(x => x.AccountType == AccountType.Deposit || x.AccountType == AccountType.Credit)
            .WithMessage("Процентная ставка обязательна для вкладов и кредитов.");
    }
}