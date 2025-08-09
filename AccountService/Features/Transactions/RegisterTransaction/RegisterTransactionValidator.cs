

using System.Diagnostics.CodeAnalysis;
using AccountService.Shared.Validation;
using FluentValidation;

namespace AccountService.Features.Transactions.RegisterTransaction;

//Resharper решил, что валидаторы не используются
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class RegisterTransactionValidator : AbstractValidator<RegisterTransactionCommand>
{
    private const int MinTransactionAmount = 0;

    private const int MaxDescriptionLength = 1000;

    public RegisterTransactionValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(MinTransactionAmount)
            .WithMessage($"Сумма перевода не может быть меньше {MinTransactionAmount}");

        RuleFor(x => x.Type)
            .MustBeValidTransactionType()
            .When(x => !string.IsNullOrEmpty(x.Type));
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Описание не может быть пустым.")
            .MaximumLength(MaxDescriptionLength).WithMessage($"Длина описания не должна превышать {MaxDescriptionLength} символов.");
    }
}