using AccountService.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AccountService.Filters;

public class ApiExceptionFilter:  IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case NotFoundException ex:
                _logger.LogError(ex.Message);
                context.Result = new NotFoundObjectResult(new { error = ex.Message });
                break;
            
            // Ошибка валидации из FluentValidation
            case ValidationException ex:
                _logger.LogError(ex.Message);
                var errors = ex.Errors.Select(err => new { property = err.PropertyName, message = err.ErrorMessage });
                context.Result = new BadRequestObjectResult(new { errors });
                break;
            
            default:
                _logger.LogError(context.Exception, context.Exception.Message);
                // И возвращаем клиенту стандартизированный ответ 500
                var details = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    Detail = "An internal server error has occurred. We are looking into it."
                };
                context.Result = new ObjectResult(details) { StatusCode = 500 };
                break;
        }

        context.ExceptionHandled = true;
    }
}