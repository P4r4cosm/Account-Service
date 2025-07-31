using FluentValidation;

namespace AccountService.Features.Transfers.CreateTransfer;

public class CreateTransferValidator: AbstractValidator<CreateTransferCommand>
{
    public CreateTransferValidator()
    {
        RuleFor(x => x.FromAccountId)
            .NotEmpty().WithMessage("ID счёта списания не может быть пустым.");

        RuleFor(x => x.ToAccountId)
            .NotEmpty().WithMessage("ID счёта зачисления не может быть пустым.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Сумма перевода должна быть больше нуля.");

        RuleFor(x => x)
            .Must(x => x.FromAccountId != x.ToAccountId)
            .WithMessage("Счёт списания и счёт зачисления не могут совпадать.")
            .WithName("Accounts"); 
    }
}