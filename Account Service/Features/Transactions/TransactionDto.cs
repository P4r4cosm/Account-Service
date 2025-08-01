namespace AccountService.Features.Transactions;

/// <summary>
/// Объект для передачи данных о транзакции.
/// </summary>
public class TransactionDto
{
    /// <summary>
    /// Уникальный идентификатор транзакции.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID счёта, к которому относится транзакция.
    /// </summary>
    public Guid AccountId { get; set; }
    
    /// <summary>
    /// ID счёта-контрагента (если это был перевод).
    /// </summary>
    public Guid? CounterpartyAccountId { get; set; }

    /// <summary>
    /// Сумма транзакции.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Валюта транзакции (ISO 4217).
    /// </summary>
    /// <example>RUB</example>
    public required string Currency { get; set; }

    /// <summary>
    /// Тип транзакции.
    /// </summary>
    /// <example>Credit</example>
    public required string Type { get; set; }

    /// <summary>
    /// Описание транзакции, предоставленное пользователем или системой.
    /// </summary>
    /// <example>Перевод на вклад "Надёжный-6"</example>
    public required string Description { get; set; }

    /// <summary>
    /// Дата и время проведения транзакции в формате UTC.
    /// </summary>
    public DateTime Timestamp { get; set; }
}