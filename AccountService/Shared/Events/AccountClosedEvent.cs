using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Событие, публикуемое при закрытии банковского счёта.
/// Routing Key: `account.closed`
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования,
                                                                    // но свойства нужны для сериализации через System.Text.Json.
[RoutingKey("account.closed")]
public class AccountClosedEvent
{
    /// <summary>
    /// Идентификатор закрытого счёта.
    /// </summary>
    [JsonPropertyName("accountId")]
    public Guid AccountId { get; set; }

    /// <summary>
    /// Идентификатор владельца счёта.
    /// </summary>
    [JsonPropertyName("ownerId")]
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Точное время (UTC) закрытия счёта.
    /// </summary>
    [JsonPropertyName("closedAt")]
    public DateTime ClosedAt { get; set; }
}