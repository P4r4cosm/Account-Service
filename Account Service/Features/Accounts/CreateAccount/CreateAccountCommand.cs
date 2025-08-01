using System.ComponentModel.DataAnnotations;
using MediatR;

namespace AccountService.Features.Accounts.CreateAccount;

/// <summary>
/// Данные для создания нового банковского счёта.
/// </summary>
public class CreateAccountCommand : IRequest<AccountDto>
{
    /// <summary>
    /// Уникальный идентификатор владельца счёта (клиента).
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    [Required] // Атрибут для валидации
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Тип создаваемого счёта. Допустимые значения: Checking, Deposit, Credit.
    /// </summary>
    /// <example>Checking</example>
    [Required]
    public required string AccountType { get; set; }

    /// <summary>
    /// Код валюты счёта в формате ISO 4217.
    /// </summary>
    /// <example>RUB</example>
    [Required]
    public required string Currency { get; set; }

    /// <summary>
    /// Процентная ставка по счёту. Обязательно для вкладов (Deposit) и кредитов (Credit).
    /// Может быть null для текущих счетов (Checking).
    /// </summary>
    /// <example>5.5</example>
    public decimal? InterestRate { get; set; }
}