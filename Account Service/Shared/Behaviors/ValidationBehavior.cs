using System.Reflection;
using AccountService.Shared.Domain;
using FluentValidation;
using FluentValidation.Results;
using MediatR;


namespace AccountService.Shared.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : MbResult
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Если валидаторов для данного запроса нет, просто продолжаем выполнение.
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        // 1. Асинхронно запускаем все валидаторы.
        ValidationResult[] validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // 2. Собираем все ошибки валидации в один список.
        List<ValidationFailure> failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // Если ошибок нет, продолжаем выполнение.
        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        // 3. Преобразуем список ошибок в словарь, как того требует MbError.
        // Используем GroupBy и First() для гарантии, что для каждого поля будет только одна ошибка,
        // что соответствует требованию "по каждому полю возвращалась только первая ошибка".
        var validationErrors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.First().ErrorMessage
            );

        // Создаем ошибку с деталями валидации.
        var error = MbError.WithValidation(validationErrors);

        // 4. Используем рефлексию для создания экземпляра TResponse с ошибкой.
        // Это самый сложный момент. Мы вызываем статический метод Failure(MbError)
        // на типе TResponse, который может быть MbResult или MbResult<TValue>.
        // Так как мы не знаем TValue во время компиляции, рефлексия - единственный способ.

        // Находим статический метод с именем "Failure", который принимает один параметр типа MbError.
        var failureMethod = typeof(TResponse).GetMethod(
            nameof(MbResult.Failure),
            BindingFlags.Public | BindingFlags.Static,
            [typeof(MbError)]
        );

        if (failureMethod is null)
        {
            // Эта ошибка означает, что наш Result-тип (TResponse) не соответствует ожидаемому контракту.
            // Например, у него нет публичного статического метода Failure(MbError).
            throw new InvalidOperationException(
                $"Тип {typeof(TResponse).Name} не имеет публичного статического метода 'Failure' с параметром типа '{nameof(MbError)}'.");
        }

        // Вызываем статический метод 'Failure(error)' и приводим результат к нужному типу TResponse.
        // Первый параметр `null` потому что метод статический.
        var validationResult = (TResponse)failureMethod.Invoke(null, [error])!;

        return validationResult;
    }
}