using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;
/// <summary>
/// Метаданные, сопровождающие каждое событие для обеспечения наблюдаемости и корректной обработки.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class EventMeta
{
    /// <summary>
    /// Версия контракта события. Позволяет потребителям обрабатывать разные версии одного события.
    /// </summary>
    /// <example>v1</example>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "v1";

    /// <summary>
    /// Имя сервиса-источника, который сгенерировал событие.
    /// </summary>
    /// <example>account-service</example>
    [JsonPropertyName("source")]
    public string Source { get; set; } = "account-service";

    /// <summary>
    /// Сквозной идентификатор для трассировки всей бизнес-операции, которая могла породить цепочку событий в разных сервисах.
    /// </summary>
    /// <example>11111111-1111-1111-1111-111111111111</example>
    [JsonPropertyName("correlationId")]
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// Идентификатор команды или события, которое послужило непосредственной причиной для генерации этого события.
    /// </summary>
    /// <example>22222222-2222-2222-2222-222222222222</example>
    [JsonPropertyName("causationId")]
    public Guid CausationId { get; set; }
}