using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Событие, публикуемое при изменении процентной ставки по счёту (вкладу).
/// Routing Key: `account.rateChanged`
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] //Resharper жалуется на неиспользуемый get, он нужен для десериализации
public class AccountInterestRateChangedEvent
{
    /// <summary>
    /// Идентификатор счёта, для которого изменилась ставка.
    /// </summary>
    [JsonPropertyName("accountId")]
    public Guid AccountId { get; set; }

    /// <summary>
    /// Предыдущее значение процентной ставки.
    /// </summary>
    [JsonPropertyName("oldRate")]
    public decimal? OldRate { get; set; }

    /// <summary>
    /// Новое значение процентной ставки.
    /// </summary>
    [JsonPropertyName("newRate")]
    public decimal? NewRate { get; set; }
}