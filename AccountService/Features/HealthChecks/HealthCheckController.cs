using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AccountService.Features.HealthChecks;



/// <summary>
/// Предоставляет эндпоинты для проверки работоспособности сервиса.
/// </summary>
[ApiController]
[AllowAnonymous]
[Produces("application/json")]
[Route("health")]
[ApiExplorerSettings(GroupName = "v1")] // Группируем в отдельный раздел Swagger
public class HealthCheckController(HealthCheckService healthCheckService) : ControllerBase
{
    /// <summary>
    /// Liveness-проверка: определяет, работает ли приложение в принципе.
    /// </summary>
    /// <remarks>
    /// Эта проверка максимально быстрая и не зависит от внешних сервисов.
    /// Если она проваливается, система оркестрации должна перезапустить контейнер.
    /// </remarks>
    /// <returns>Статус 200 OK, если сервис запущен.</returns>
    [HttpGet("live")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Live()
    {
        // Для Liveness-проверки мы вызываем сервис с предикатом, который отсекает все проверки.
        // Нам важно лишь то, что сервис смог ответить на запрос.
        var report = await healthCheckService.CheckHealthAsync(_ => false);
        var response = HealthCheckResponse.FromHealthReport(report);
        
        return Ok(response);
    }
    
    /// <summary>
    /// Readiness-проверка: определяет, готов ли сервис принимать трафик.
    /// </summary>
    /// <remarks>
    /// Этот эндпоинт используется дашбордом HealthChecksUI.
    /// Он возвращает детальный JSON в формате, совместимом с HealthChecksUI.
    /// </remarks>
    [HttpGet("ready")]
    public async Task Ready()
    {
        // Для Readiness-проверки мы запускаем все зарегистрированные проверки.
        var report = await healthCheckService.CheckHealthAsync();

        // В соответствии с контрактом readiness-проб, если статус не Healthy,
        // эндпоинт должен вернуть код ответа, отличный от 2xx. 503 - стандартный выбор.
        await UIResponseWriter.WriteHealthCheckUIResponse(HttpContext, report);
        //return report.Status == HealthStatus.Healthy ? Ok(response) : StatusCode((int)HttpStatusCode.ServiceUnavailable, response);
    }
}