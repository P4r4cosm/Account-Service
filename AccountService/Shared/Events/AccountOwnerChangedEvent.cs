using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
public class AccountOwnerChangedEvent
{
    [JsonPropertyName("accountId")]
    public Guid AccountId { get; set; }

    [JsonPropertyName("oldOwnerId")]
    public Guid OldOwnerId { get; set; }

    [JsonPropertyName("newOwnerId")]
    public Guid NewOwnerId { get; set; }
}