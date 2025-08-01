using AccountService.Features.Accounts;
using AccountService.Features.Transactions;
using AccountService.Infrastructure.Verification;
using FluentValidation;

namespace AccountService.Core.Validation;

public static class ValidationExtensions
{
    private static readonly IReadOnlyCollection<string> _validAccountTypes =
        Enum.GetNames(typeof(AccountType)).ToList().AsReadOnly();
    
    private static readonly IReadOnlyCollection<string> _validTransactionType =
        Enum.GetNames(typeof(TransactionType)).ToList().AsReadOnly();

    /// <summary>
    /// Проверяет, что строковое представление типа счёта является допустимым.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeValidAccountType<T>(this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(type =>
            {
                if (string.IsNullOrEmpty(type))
                {
                    return true; // Не валидируем пустые значения, для этого есть .NotEmpty()
                }

                // Проверка без учета регистра
                return _validAccountTypes.Any(validType => validType.Equals(type, StringComparison.OrdinalIgnoreCase));
            })
            .WithMessage("Указан некорректный тип счёта '{PropertyValue}'. " +
                         $"Допустимые значения: {string.Join(", ", _validAccountTypes)}");
    }

    public static IRuleBuilderOptions<T, string> MustBeValidCurrency<T>(this IRuleBuilder<T, string> ruleBuilder,
        ICurrencyService currencyService)
    {
        return ruleBuilder
            .Length(3)
            .MustAsync(async (currency, cancellationToken) =>
            {
                if (string.IsNullOrEmpty(currency)) return true;
                return await currencyService.IsSupportedAsync(currency, cancellationToken);
            })
            .WithMessage("Валюта '{PropertyValue}' не поддерживается системой.");
    }

    public static IRuleBuilderOptions<T, string?> MustBeValidTransactionType<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder.Must(type =>
        {
            if (string.IsNullOrEmpty(type))
            {
                return true;
            }
            return _validTransactionType.Any(validType => validType.Equals(type, StringComparison.OrdinalIgnoreCase));
        }).WithMessage("Указан некорректный тип счёта '{PropertyValue}'. " +
                       $"Допустимые значения: {string.Join(", ", _validTransactionType)}");
    }
}