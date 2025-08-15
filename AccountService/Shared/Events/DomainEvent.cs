using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

public class DomainEvent
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; }

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; }

    [JsonPropertyName("meta")]
    public EventMeta Meta { get; }

    protected DomainEvent(Guid correlationId, Guid causationId)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        Meta = new EventMeta
        {
            CorrelationId = correlationId,
            CausationId = causationId
        };
    }
}