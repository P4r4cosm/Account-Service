namespace AccountService.Features.Accounts;

/// <summary>
/// Тип банковского счёта.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Текущий (расчётный) счёт.
    /// </summary>
    Checking,

    /// <summary>
    /// Вклад (депозит).
    /// </summary>
    Deposit,

    /// <summary>
    /// Кредитный счёт.
    /// </summary>
    Credit
}