using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Событие, публикуемое при успешном зачислении денежных средств на счёт.
/// Предназначено для сервиса уведомлений и других систем.
/// Routing Key: `money.credited`
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
[RoutingKey("money.credited")]
public class MoneyCreditedEvent
{
    /// <summary>
    /// Идентификатор счёта, на который зачислены средства.
    /// </summary>
    [JsonPropertyName("accountId")] public Guid AccountId { get; set; }

    /// <summary>
    /// Сумма зачисления в валюте счёта.
    /// </summary>
    /// <example>1000.00</example>
    [JsonPropertyName("amount")] public decimal Amount { get; set; }

    /// <summary>
    /// Код валюты счёта в формате ISO 4217.
    /// </summary>
    /// <example>RUB</example>
    [JsonPropertyName("currency")] public required string Currency { get; set; }

    /// <summary>
    /// Уникальный идентификатор финансовой операции пополнения.
    /// </summary>
    [JsonPropertyName("operationId")] public Guid OperationId { get; set; }
}