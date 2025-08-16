using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

[SuppressMessage("ReSharper",
    "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
public class TransferCompletedEvent
{
    [JsonPropertyName("sourceAccountId")] public Guid SourceAccountId { get; set; }

    [JsonPropertyName("destinationAccountId")]
    public Guid DestinationAccountId { get; set; }

    [JsonPropertyName("amount")] public decimal Amount { get; set; }

    [JsonPropertyName("currency")] public required string Currency { get; set; }

    [JsonPropertyName("transferId")] public Guid TransferId { get; set; }
}