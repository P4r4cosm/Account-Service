namespace AccountService.Shared.Domain;

/// <summary>
/// Представляет результат операции без возвращаемого значения.
/// </summary>
public class MbResult
{
    /// <summary>
    /// Указывает, успешен ли результат.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Указывает, является ли результат ошибочным.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Ошибка, если результат неуспешен.
    /// </summary>
    public MbError? Error { get; }

    protected MbResult(bool isSuccess, MbError? error)
    {
        switch (isSuccess)
        {
            case true when error is not null:
                throw new InvalidOperationException("Успешный результат не может содержать ошибку.");
            case false when error is null:
                throw new InvalidOperationException("Неуспешный результат должен содержать ошибку.");
            default:
                IsSuccess = isSuccess;
                Error = error;
                break;
        }
    }
    
    /// <summary>
    /// Создает успешный результат.
    /// </summary>
    public static MbResult Success() => new(true, null);

    /// <summary>
    /// Создает ошибочный результат.
    /// </summary>
    public static MbResult Failure(MbError error) => new(false, error);
}