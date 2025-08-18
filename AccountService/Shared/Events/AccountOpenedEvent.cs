using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Событие, публикуемое при успешном открытии нового банковского счёта.
/// Предназначено для информирования других систем, например, CRM.
/// Routing Key: `account.opened`
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
[RoutingKey("account.opened")]
public class AccountOpenedEvent
{
    /// <summary>
    /// Уникальный идентификатор нового счёта.
    /// </summary>
    [JsonPropertyName("accountId")] public Guid AccountId { get; set; }
    
    /// <summary>
    /// Уникальный идентификатор клиента-владельца счёта.
    /// </summary>
    [JsonPropertyName("ownerId")] public Guid OwnerId { get; init; }

    /// <summary>
    /// Код валюты счёта в формате ISO 4217.
    /// </summary>
    /// <example>RUB</example>
    [JsonPropertyName("currency")] public required string Currency { get; set; }
    
    /// <summary>
    /// Тип открытого счёта.
    /// </summary>
    /// <example>Checking</example>
    [JsonPropertyName("type")] public required string Type { get; set; }
}