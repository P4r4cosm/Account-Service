using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

public class TransferCompletedEvent(Guid correlationId, Guid causationId) : DomainEvent(correlationId, causationId)
{
    [JsonPropertyName("sourceAccountId")]
    public Guid SourceAccountId { get; set; }

    [JsonPropertyName("destinationAccountId")]
    public Guid DestinationAccountId { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public required string Currency { get; set; }

    [JsonPropertyName("transferId")]
    public Guid TransferId { get; set; }
}