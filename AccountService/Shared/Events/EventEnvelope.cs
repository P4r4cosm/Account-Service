using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

public class EventEnvelope<T>
    where T : class
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; }

    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; init; }

    [JsonPropertyName("payload")]
    public T Payload { get; init; }

    [JsonPropertyName("meta")]
    public EventMeta Meta { get; init; }

    /// <summary>
    /// Этот конструктор  используется  (например, в CreateAccountHandler)
    /// для удобного СОЗДАНИЯ новых событий.
    /// Он автоматически генерирует EventId, OccurredAt и собирает объект Meta.
    /// </summary>
    public EventEnvelope(T payload, Guid correlationId, Guid causationId)
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        Payload = payload;
        Meta = new EventMeta
        {
            Version = "v1",
            Source = "account-service",
            CorrelationId = correlationId,
            CausationId = causationId
        };
    }

    /// <summary>
    /// Этот конструктор  будет использоваться ТОЛЬКО десериализатором System.Text.Json.
    /// Его параметры (eventId, occurredAt, payload, meta) в точности соответствуют
    /// свойствам верхнего уровня в вашем JSON.
    /// </summary>
    [JsonConstructor]
    public EventEnvelope(Guid eventId, DateTime occurredAt, T payload, EventMeta meta)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
        Payload = payload;
        Meta = meta;
    }
}