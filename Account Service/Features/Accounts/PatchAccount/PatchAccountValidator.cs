using FluentValidation;

namespace AccountService.Features.Accounts.PatchAccount;

public class PatchAccountCommandValidator : AbstractValidator<PatchAccountCommand>
{
    public PatchAccountCommandValidator()
    {
        // Проверяем OwnerId, только если он не null
        When(cmd => cmd.OwnerId.HasValue, () =>
        {
            RuleFor(cmd => cmd.OwnerId)
                .NotEmpty().WithMessage("ID владельца не может быть пустым.");
        });

        // Проверяем InterestRate, только если он не null
        When(cmd => cmd.InterestRate.HasValue, () =>
        {
            RuleFor(cmd => cmd.InterestRate)
                .GreaterThanOrEqualTo(0).WithMessage("Процентная ставка не может быть отрицательной.");
        });
    }
}