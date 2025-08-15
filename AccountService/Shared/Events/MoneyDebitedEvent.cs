using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

public class MoneyDebitedEvent(Guid correlationId, Guid causationId) : DomainEvent(correlationId, causationId)
{
    [JsonPropertyName("accountId")]
    public Guid AccountId { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public required string Currency { get; set; }

    [JsonPropertyName("operationId")]
    public Guid OperationId { get; set; }
    
    [JsonPropertyName("reason")]
    public required string Reason { get; set; }

   
}