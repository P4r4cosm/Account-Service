using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Событие, публикуемое после успешного начисления процентов на остаток по вкладу.
/// Routing Key: `money.interest.accrued`
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
public class InterestAccruedEvent
{
    /// <summary>
    /// Идентификатор счёта, на который начислены проценты.
    /// </summary>
    [JsonPropertyName("accountId")] public Guid AccountId { get; set; }

    /// <summary>
    /// Сумма начисленных процентов.
    /// </summary>
    /// <example>123.45</example>
    [JsonPropertyName("amount")] public decimal Amount { get; set; }

    /// <summary>
    /// Начало периода, за который начислены проценты.
    /// </summary>
    [JsonPropertyName("periodFrom")] public DateTime PeriodFrom { get; set; }

    /// <summary>
    /// Конец периода, за который начислены проценты.
    /// </summary>
    [JsonPropertyName("periodTo")] public DateTime PeriodTo { get; set; }
}