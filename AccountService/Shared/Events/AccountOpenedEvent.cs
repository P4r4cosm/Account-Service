using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

public class AccountOpenedEvent(Guid correlationId, Guid causationId) : DomainEvent(correlationId, causationId)
{
    [JsonPropertyName("accountId")] public Guid AccountId { get; set; }

    [JsonPropertyName("ownerId")] public Guid OwnerId { get; set; }

    [JsonPropertyName("currency")] public required string Currency { get; set; }

    [JsonPropertyName("type")] public required string Type { get; set; }
}