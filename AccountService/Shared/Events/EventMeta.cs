using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

public class EventMeta
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "v1";

    [JsonPropertyName("source")]
    public string Source { get; set; } = "account-service";

    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; set; }

    [JsonPropertyName("causationId")]
    public Guid CausationId { get; set; }
}