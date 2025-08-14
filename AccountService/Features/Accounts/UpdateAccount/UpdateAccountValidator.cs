using System.Diagnostics.CodeAnalysis;
using FluentValidation;

namespace AccountService.Features.Accounts.UpdateAccount;

//Resharper решил, что валидаторы не используются
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class UpdateAccountValidator:  AbstractValidator<UpdateAccountCommand>
{
    
    public UpdateAccountValidator()
    {
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("ID счёта не может быть пустым.");
        // проверка guid счёта
        RuleFor(x => x.AccountId)
            .NotEmpty().WithMessage("ID счёта не может быть пустым.");
        
        // проверка процентной ставки
        RuleFor(x => x.InterestRate)
            .GreaterThanOrEqualTo(0).When(x => x.InterestRate.HasValue)
            .WithMessage("Процентная ставка не может быть отрицательной.");
        
    }
}