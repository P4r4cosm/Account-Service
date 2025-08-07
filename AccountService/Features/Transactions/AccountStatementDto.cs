using System.Diagnostics.CodeAnalysis;
using AccountService.Shared.Domain;

namespace AccountService.Features.Transactions;

/// <summary>
/// Представляет полную выписку по счёту, включая информацию о самом счёте и пагинированный список транзакций.
/// </summary>

[SuppressMessage("ReSharper", "UnusedMember.Global")]
// Resharper жалуется на "неиспользуемые" поля, т.к. Dto заполняется через AutoMapper, 
//то в коде они напрямую не используются
public class AccountStatementDto
{
    /// <summary>
    /// ID счёта.
    /// </summary>
    public Guid AccountId { get; init; }

    /// <summary>
    /// ID владельца счёта.
    /// </summary>
    public Guid OwnerId { get; init; }

    /// <summary>
    /// Тип счёта (например, "Checking", "Deposit").
    /// </summary>
    public required string AccountType { get; init; }

    /// <summary>
    ///* Текущий баланс счёта.
    /// </summary>
    public decimal CurrentBalance { get; init; }

    /// <summary>
    /// Валюта счёта (ISO 4217).
    /// </summary>
    public required string Currency { get; init; }
    
    /// <summary>
    /// Дата открытия счёта.
    /// </summary>
    public DateTime OpenedDate { get; init; }

    /// <summary>
    /// Пагинированный список транзакций за запрошенный период.
    /// </summary>
    public required PagedResult<TransactionDto> Transactions { get; set; }
}