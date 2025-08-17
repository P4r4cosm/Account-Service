using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AccountService.Features.HealthChecks;

/// <summary>
/// Представляет собой форматированный ответ от эндпоинта проверки работоспособности.
/// </summary>
public class HealthCheckResponse
{
    /// <summary>
    /// Общий статус работоспособности сервиса.
    /// </summary>
    /// <example>Healthy</example>
    public required string OverallStatus { get; init; }

    /// <summary>
    /// Список результатов по каждой отдельной проверке.
    /// </summary>
    public required IEnumerable<HealthCheckEntry> Checks { get; init; }

    /// <summary>
    /// Общее время, затраченное на выполнение всех проверок.
    /// </summary>
    /// <example>00:00:00.1234567</example>
    public TimeSpan TotalDuration { get; init; }

    // Статический метод-фабрика для удобного создания DTO из стандартного HealthReport
    public static HealthCheckResponse FromHealthReport(HealthReport report)
    {
        return new HealthCheckResponse
        {
            OverallStatus = report.Status.ToString(),
            TotalDuration = report.TotalDuration,
            Checks = report.Entries.Select(e => new HealthCheckEntry
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                Description = e.Value.Description,
                Duration = e.Value.Duration,
                ErrorMessage = e.Value.Exception?.Message
            })
        };
    }
}

