using System.Text.Json.Serialization;

namespace AccountService.Shared.Domain;

/// <summary>
/// Представляет результат операции с возвращаемым значением.
/// </summary>
/// <typeparam name="TValue">Тип возвращаемого значения.</typeparam>
public class MbResult<TValue> : MbResult
{
   
    /// <summary>
    /// Значение успешного результата.
    /// Будет null в случае ошибки.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TValue? Value { get; }

    protected MbResult(TValue? value, bool isSuccess, MbError? error)
        : base(isSuccess, error)
    {
        // Присваиваем значение напрямую публичному свойству.
        Value = value;
    }
    
    /// <summary>
    /// Создает успешный результат со значением.
    /// </summary>
    public static MbResult<TValue> Success(TValue value) => new(value, true, null);

    /// <summary>
    /// Создает ошибочный результат.
    /// </summary>
    public new static MbResult<TValue> Failure(MbError error) => new(default, false, error);

    // Неявные преобразования
    public static implicit operator MbResult<TValue>(TValue value) => Success(value);
    public static implicit operator MbResult<TValue>(MbError error) => Failure(error);
}