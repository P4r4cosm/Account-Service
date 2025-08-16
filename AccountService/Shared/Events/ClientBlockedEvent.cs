using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

public class ClientBlockedEvent
{
    [JsonPropertyName("clientId")]
    public Guid ClientId { get; set; }
}