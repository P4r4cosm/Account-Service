using System.Text.Json.Serialization;
using MediatR;

namespace AccountService.Features.Transactions.RegisterTransaction;

/// <summary>
/// Команда для регистрации одной транзакции (пополнения или списания) по счёту.
/// </summary>
public class RegisterTransactionCommand : IRequest<TransactionDto>
{
    /// <summary>
    /// ID счёта, по которому проводится транзакция.
    /// </summary>
    [JsonIgnore] // Берётся из URL, а не из тела
    public Guid AccountId { get; set; }

    /// <summary>
    /// Сумма транзакции. Должна быть положительной.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Тип транзакции: "Credit" (зачисление) или "Debit" (списание).
    /// </summary>
    /// <example>Credit</example>
    public string Type { get; set; }

    /// <summary>
    /// Описание транзакции.
    /// </summary>
    /// <example>Пополнение наличными в кассе</example>
    public string Description { get; set; }
}