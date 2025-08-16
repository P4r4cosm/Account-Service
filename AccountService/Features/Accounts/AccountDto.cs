using System.Diagnostics.CodeAnalysis;

namespace AccountService.Features.Accounts;

/// <summary>
/// Объект для передачи данных о счёте клиенту (DTO - Data Transfer Object).
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
// Resharper жалуется на "неиспользуемые" поля, т.к. Dto заполняется через AutoMapper, 
//то в коде они напрямую не используются
public class AccountDto
{
    /// <summary>
    /// Уникальный идентификатор счёта (GUID).
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; }

    /// <summary>
    /// Идентификатор владельца счёта (клиента).
    /// </summary>
    /// <example>a1b2c3d4-e5f6-7890-1234-567890abcdef</example>
    public Guid OwnerId { get; init; }

    /// <summary>
    /// Тип счёта. Возможные значения: Checking, Deposit, Credit.
    /// </summary>
    /// <example>Deposit</example>
    public required string AccountType { get; init; }

    /// <summary>
    /// Код валюты в формате ISO 4217.
    /// </summary>
    /// <example>RUB</example>
    public required string Currency { get; init; }

    /// <summary>
    /// Текущий баланс счёта.
    /// </summary>
    /// <example>150750.25</example>
    public decimal Balance { get; init; }

    /// <summary>
    /// Процентная ставка по счёту. Может быть null, если не применима к данному типу счёта (например, для текущего счёта).
    /// </summary>
    /// <example>4.5</example>
    public decimal? InterestRate { get; init; }

    /// <summary>
    /// Дата и время открытия счёта (в формате UTC).
    /// </summary>
    /// <example>2024-05-20T10:30:00Z</example>
    public DateTime OpenedDate { get; init; }

    /// <summary>
    /// Дата и время закрытия счёта (в формате UTC). Будет null, если счёт активен.
    /// </summary>
    /// <example>null</example>
    public DateTime? CloseDate { get; init; }

    /// <summary>
    /// Флаг, указывающий, что счет заморожен.
    /// Расходные операции по такому счету запрещены.
    /// </summary>
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global  Resharper жалуется на "неиспользуемый" set, т.к. Dto заполняется через AutoMapper,
    // то в коде они напрямую не используются
    public bool IsFrozen { get; set; } = false; // По умолчанию счет не заморожен
}