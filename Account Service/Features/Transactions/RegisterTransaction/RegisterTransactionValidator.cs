using System.Data;
using AccountService.Core.Validation;
using FluentValidation;

namespace AccountService.Features.Transactions.RegisterTransaction;

public class RegisterTransactionValidator : AbstractValidator<RegisterTransactionCommand>
{
    private static int _minTransactionAmmount = 0;

    private static int _maxDescriptionLength = 200;
    
    public RegisterTransactionValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(_minTransactionAmmount)
            .WithMessage($"Сумма перевода не может быть меньше {_minTransactionAmmount}");

        RuleFor(x => x.Type)
            .MustBeValidTransactionType()
            .When(x => !string.IsNullOrEmpty(x.Type));
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Описание не может быть пустым.")
            .MaximumLength(_maxDescriptionLength).WithMessage($"Длина описания не должна превышать {_maxDescriptionLength} символов.");
    }
}