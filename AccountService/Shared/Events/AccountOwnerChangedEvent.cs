using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Событие, публикуемое при смене владельца счёта.
/// Routing Key: `account.ownerChanged`
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public class AccountOwnerChangedEvent
{
    /// <summary>
    /// Идентификатор счёта, у которого сменился владелец.
    /// </summary>
    [JsonPropertyName("accountId")]
    public Guid AccountId { get; set; }

    /// <summary>
    /// Уникальный идентификатор предыдущего владельца счёта.
    /// </summary>
    [JsonPropertyName("oldOwnerId")]
    public Guid OldOwnerId { get; set; }

    /// <summary>
    /// Уникальный идентификатор нового владельца счёта.
    /// </summary>
    [JsonPropertyName("newOwnerId")]
    public Guid NewOwnerId { get; set; }
}