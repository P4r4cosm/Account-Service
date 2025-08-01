using AccountService.Shared.Domain;

namespace AccountService.Features.Transactions;

/// <summary>
/// Представляет полную выписку по счёту, включая информацию о самом счёте и пагинированный список транзакций.
/// </summary>
public class AccountStatementDto
{
    /// <summary>
    /// ID счёта.
    /// </summary>
    public Guid AccountId { get; set; }

    /// <summary>
    /// ID владельца счёта.
    /// </summary>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Тип счёта (например, "Checking", "Deposit").
    /// </summary>
    public required string AccountType { get; set; }

    /// <summary>
    ///* Текущий баланс счёта.
    /// </summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>
    /// Валюта счёта (ISO 4217).
    /// </summary>
    public required string Currency { get; set; }
    
    /// <summary>
    /// Дата открытия счёта.
    /// </summary>
    public DateTime OpenedDate { get; set; }

    /// <summary>
    /// Пагинированный список транзакций за запрошенный период.
    /// </summary>
    public required PagedResult<TransactionDto> Transactions { get; set; }
}