using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
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