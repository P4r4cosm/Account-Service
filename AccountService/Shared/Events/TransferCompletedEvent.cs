using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Событие, публикуемое при успешном завершении перевода между двумя счетами.
/// Routing Key: `money.transfer.completed`
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
public class TransferCompletedEvent
{
    /// <summary>
    /// Идентификатор счёта-источника списания.
    /// </summary>
    [JsonPropertyName("sourceAccountId")] public Guid SourceAccountId { get; set; }

    /// <summary>
    /// Идентификатор счёта-получателя зачисления.
    /// </summary>
    [JsonPropertyName("destinationAccountId")] public Guid DestinationAccountId { get; set; }

    /// <summary>
    /// Сумма перевода.
    /// </summary>
    /// <example>200.00</example>
    [JsonPropertyName("amount")] public decimal Amount { get; set; }

    /// <summary>
    /// Код валюты перевода в формате ISO 4217.
    /// </summary>
    /// <example>RUB</example>
    [JsonPropertyName("currency")] public required string Currency { get; set; }

    /// <summary>
    /// Уникальный идентификатор операции перевода.
    /// </summary>
    [JsonPropertyName("transferId")] public Guid TransferId { get; set; }
}