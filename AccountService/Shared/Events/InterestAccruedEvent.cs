using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
public class InterestAccruedEvent
{
    [JsonPropertyName("accountId")] public Guid AccountId { get; set; }

    [JsonPropertyName("amount")] public decimal Amount { get; set; }

    [JsonPropertyName("periodFrom")] public DateTime PeriodFrom { get; set; }

    [JsonPropertyName("periodTo")] public DateTime PeriodTo { get; set; }
}