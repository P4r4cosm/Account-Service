using JetBrains.Annotations;

namespace AccountService.Shared.Domain;

/// <summary>
/// Представляет ошибку операции.
/// </summary>
[PublicAPI] // Сообщаем ReSharper, что все публичные члены этого класса используются извне (сериализатором)
public class MbError
{
    /// <summary>
    /// Код ошибки.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Описание ошибки.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Словарь с ошибками валидации (поле -> сообщение).
    /// </summary>
    public IReadOnlyDictionary<string, string>? ValidationErrors { get; }

    /// <summary>
    /// Приватный конструктор для предотвращения прямого создания.
    /// Используйте статические методы для создания экземпляров.
    /// </summary>
    private MbError(string code, string description, IReadOnlyDictionary<string, string>? validationErrors = null)
    {
        Code = code;
        Description = description;
        ValidationErrors = validationErrors;
    }

    /// <summary>
    /// Создает кастомную ошибку.
    /// </summary>
    public static MbError Custom(string code, string description) => new(code, description);

    /// <summary>
    /// Создает ошибку валидации с деталями.
    /// </summary>
    public static MbError WithValidation(IReadOnlyDictionary<string, string> errors) =>
        new("General.Validation", "Одна или несколько ошибок валидации.", errors);
}