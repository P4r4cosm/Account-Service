using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

public class ClientUnblockedEvent
{
    [JsonPropertyName("clientId")]
    public Guid ClientId { get; set; }
}