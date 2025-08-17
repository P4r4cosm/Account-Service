using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Событие, публикуемое при успешном списании денежных средств со счёта.
/// Routing Key: `money.debited`
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class MoneyDebitedEvent
{
    /// <summary>
    /// Идентификатор счёта, с которого списаны средства.
    /// </summary>
    [JsonPropertyName("accountId")] public Guid AccountId { get; set; }

    /// <summary>
    /// Сумма списания в валюте счёта.
    /// </summary>
    /// <example>200.50</example>
    [JsonPropertyName("amount")] public decimal Amount { get; set; }

    /// <summary>
    /// Код валюты счёта в формате ISO 4217.
    /// </summary>
    /// <example>RUB</example>
    [JsonPropertyName("currency")] public required string Currency { get; set; }

    /// <summary>
    /// Уникальный идентификатор финансовой операции списания.
    /// </summary>
    [JsonPropertyName("operationId")] public Guid OperationId { get; set; }
    
    /// <summary>
    /// Причина списания.
    /// </summary>
    /// <example>Transfer</example>
    [JsonPropertyName("reason")] public required string Reason { get; set; }
}