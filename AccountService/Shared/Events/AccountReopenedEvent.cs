using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования,
                                                                    // но свойства нужны для сериализации через System.Text.Json.
[RoutingKey("account.reopened")]
public class AccountReopenedEvent
{
    /// <summary>
    /// ID счёта.
    /// </summary>
    [JsonPropertyName("accountId")]
    public Guid AccountId { get; set; }
    /// <summary>
    /// Уникальный идентификатор владельца счёта (клиента).
    /// </summary>

    [JsonPropertyName("ownerId")]
    public Guid OwnerId { get; set; }
}