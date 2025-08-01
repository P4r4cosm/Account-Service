using System.Diagnostics.CodeAnalysis;

namespace AccountService.Features.Transactions;

/// <summary>
/// Объект для передачи данных о транзакции.
/// </summary>

[SuppressMessage("ReSharper", "UnusedMember.Global")]
// Resharper жалуется на "неиспользуемые" поля, т.к. Dto заполняется через AutoMapper, 
//то в коде они напрямую не используются
public class TransactionDto
{
    /// <summary>
    /// Уникальный идентификатор транзакции.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// ID счёта, к которому относится транзакция.
    /// </summary>
    public Guid AccountId { get; init; }
    
    /// <summary>
    /// ID счёта-контрагента (если это был перевод).
    /// </summary>
    public Guid? CounterpartyAccountId { get; init; }

    /// <summary>
    /// Сумма транзакции.
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Валюта транзакции (ISO 4217).
    /// </summary>
    /// <example>RUB</example>
    public required string Currency { get; init; }

    /// <summary>
    /// Тип транзакции.
    /// </summary>
    /// <example>Credit</example>
    public required string Type { get; init; }

    /// <summary>
    /// Описание транзакции, предоставленное пользователем или системой.
    /// </summary>
    /// <example>Перевод на вклад "Надёжный-6"</example>
    public required string Description { get; init; }

    /// <summary>
    /// Дата и время проведения транзакции в формате UTC.
    /// </summary>
    public DateTime Timestamp { get; init; }
}