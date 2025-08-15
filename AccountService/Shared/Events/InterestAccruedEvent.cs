using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

public class InterestAccruedEvent
{
    [JsonPropertyName("accountId")] public Guid AccountId { get; set; }

    [JsonPropertyName("amount")] public decimal Amount { get; set; }

    [JsonPropertyName("periodFrom")] public DateTime PeriodFrom { get; set; }

    [JsonPropertyName("periodTo")] public DateTime PeriodTo { get; set; }
}