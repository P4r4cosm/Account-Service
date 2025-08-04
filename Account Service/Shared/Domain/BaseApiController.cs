using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AccountService.Shared.Domain;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;

    protected BaseApiController(IMediator mediator)
    {
        Mediator = mediator;
    }

    // Перегрузка для операций без возвращаемого значения (например, DELETE)
    protected IActionResult HandleResult(MbResult result)
    {
        return result.IsSuccess
            ?
            // Для операций типа DELETE успешно выполненных, можно вернуть 200 ОК с
            // успешным MbResult в теле или 204 NoContent.
            // Чтобы выполнить требование ТЗ (возврат MbResult), выбираем 200 OK.
            Ok(result)
            : HandleFailure(result);
    }

    private IActionResult HandleFailure(MbResult result)
    {
        // Проверяем, что ошибка действительно есть
        if (result.IsSuccess || result.Error is null)
        {
            // Этого не должно происходить, но лучше подстраховаться.
            return StatusCode(StatusCodes.Status500InternalServerError,
                MbResult.Failure(MbError.Custom("Error.Handling", "Произошла ошибка при обработке другой ошибки.")));
        }

        // В зависимости от кода ошибки выбираем соответствующий HTTP-статус.
        // В тело ответа всегда помещаем полный объект MbResult.
        // Используем switch с конструкцией 'when' для проверки суффикса
        return result.Error.Code switch
        {
            // Если код ошибки заканчивается на ".Validation", возвращаем 400
            var code when code.EndsWith(".Validation", StringComparison.OrdinalIgnoreCase)
                => BadRequest(result),

            // Если код ошибки заканчивается на ".NotFound", возвращаем 404
            var code when code.EndsWith(".NotFound", StringComparison.OrdinalIgnoreCase)
                => NotFound(result),

            // Если код ошибки заканчивается на ".Forbidden" или ".Auth", возвращаем 403
            var code when code.EndsWith(".Forbidden", StringComparison.OrdinalIgnoreCase) ||
                          code.EndsWith(".Auth", StringComparison.OrdinalIgnoreCase)
                => Forbid(), // Forbid() возвращает 403

            // Если код ошибки указывает на конфликт (например, попытка создать дубликат)
            var code when code.EndsWith(".Conflict", StringComparison.OrdinalIgnoreCase)
                => Conflict(result), // Conflict() возвращает 409

            // Все остальные ошибки считаем внутренней ошибкой сервера
            _ => StatusCode(StatusCodes.Status500InternalServerError, result)
        };
    }
}