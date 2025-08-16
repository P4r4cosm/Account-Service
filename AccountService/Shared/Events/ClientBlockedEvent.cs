using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
// ReSharper disable once ClassNeverInstantiated.Global 
public class ClientBlockedEvent
{
    [JsonPropertyName("clientId")]
    public Guid ClientId { get; set; }
}