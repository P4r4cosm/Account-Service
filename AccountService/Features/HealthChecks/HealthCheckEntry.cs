using System.Diagnostics.CodeAnalysis;

namespace AccountService.Features.HealthChecks;

/// <summary>
/// Представляет результат одной конкретной проверки работоспособности.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] //Re
public class HealthCheckEntry
{
    /// <summary>
    /// Имя проверки (например, "PostgreSQL" или "Outbox Lag").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Статус проверки.
    /// </summary>
    /// <example>Healthy</example>
    public required string Status { get; init; }

    /// <summary>
    /// Описание, возвращаемое проверкой.
    /// </summary>
    /// <example>Outbox в порядке. Необработанных сообщений: 10.</example>
    public string? Description { get; init; }

    /// <summary>
    /// Сообщение об ошибке, если проверка провалилась с исключением.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Время, затраченное на выполнение проверки.
    /// </summary>
    public TimeSpan Duration { get; init; }
}