namespace BankAccounts.Features.Transactions;

/// <summary>
/// Тип финансовой транзакции.
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Зачисление средств на счёт (приход).
    /// </summary>
    Credit,

    /// <summary>
    /// Списание средств со счёта (расход).
    /// </summary>
    Debit
}