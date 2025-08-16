using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
public class MoneyCreditedEvent
{
    [JsonPropertyName("accountId")] public Guid AccountId { get; set; }

    [JsonPropertyName("amount")] public decimal Amount { get; set; }

    [JsonPropertyName("currency")] public required string Currency { get; set; }

    [JsonPropertyName("operationId")] public Guid OperationId { get; set; }
    
}