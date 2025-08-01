using AccountService.Shared.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AccountService.Shared.Filters;

// ReSharper disable once ClassNeverInstantiated.Global Resharper считает что класс не создаётся, хотя он зарегистрирован как фильтр контроллеров
public class ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case NotFoundException ex:
                logger.LogError(ex, "Entity not found. Message: {ErrorMessage}", ex.Message);
                context.Result = new NotFoundObjectResult(new { error = ex.Message });
                break;

            case ValidationException ex:
                logger.LogError(ex, "Validation failed. Message: {ErrorMessage}", ex.Message);
                var errors = ex.Errors.Select(err => new { property = err.PropertyName, message = err.ErrorMessage });
                context.Result = new BadRequestObjectResult(new { errors });
                break;

            case OperationNotAllowedException ex:
                logger.LogError(ex, "Operation not allowed. Message: {ErrorMessage}", ex.Message);
                context.Result = new BadRequestObjectResult(new { error = ex.Message });
                break;

            default:
                logger.LogError(context.Exception, "An unexpected error occurred.");
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