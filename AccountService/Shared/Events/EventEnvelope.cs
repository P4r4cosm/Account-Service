using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Стандартная оболочка для всех доменных событий.
/// Обеспечивает единую структуру для метаданных и самого тела события.
/// </summary>
/// <typeparam name="T">Тип полезной нагрузки (payload) события.</typeparam>
public class EventEnvelope<T>
    where T : class
{
    /// <summary>
    /// Уникальный идентификатор самого события (не путать с ID сущности).
    /// </summary>
    /// <example>b5f3a7f6-2f4e-4b1a-9f3a-2b0c1e7c1a11</example>
    [JsonPropertyName("eventId")]
    public Guid EventId { get; init; }

    /// <summary>
    /// Точное время (UTC), когда произошло событие.
    /// </summary>
    /// <example>2025-08-12T21:00:00Z</example>
    [JsonPropertyName("occurredAt")]
    public DateTime OccurredAt { get; init; }

    /// <summary>
    /// Тело события — объект с данными, специфичными для этого типа события.
    /// </summary>
    [JsonPropertyName("payload")]
    public T Payload { get; init; }

    /// <summary>
    /// Метаданные, необходимые для трассировки, версионирования и маршрутизации.
    /// </summary>
    [JsonPropertyName("meta")] public EventMeta Meta { get; init; }

    /// <summary>
    /// Этот конструктор используется (например, в CreateAccountHandler)
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
    /// Этот конструктор будет использоваться ТОЛЬКО десериализатором System.Text.Json.
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