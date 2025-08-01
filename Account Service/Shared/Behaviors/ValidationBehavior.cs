using AccountService.Features.Accounts.CreateAccount;
using MediatR;
using FluentValidation;

namespace AccountService.Core.Behaviors;

/// <summary>
/// Pipeline-поведение для MediatR, которое автоматически выполняет валидацию
/// для всех входящих команд и запросов, у которых есть валидатор.
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
// Ограничение: TRequest должен быть командой/запросом MediatR
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }


    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // 1. Проверяем, есть ли вообще валидаторы для этого запроса
        if (!_validators.Any())
        {
            // Передаём управление дальше
            return await next();
        }

        // 2. Создаем контекст валидации
        var context = new ValidationContext<TRequest>(request);

        // 3. запускаем все валидаторы и собираем результаты
        var validationResults =
            await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));


        // 4. Собираем все ошибки из всех валидаторов в один список
        var failures = validationResults.SelectMany(v => v.Errors).ToList();

        // 5. Если нашлась ошибка/и
        if (failures.Any())
        {
            throw new ValidationException(failures);
        }

        // 6. Если ошибок нет, передаем управление дальше по конвейеру.
        return await next();
    }
}