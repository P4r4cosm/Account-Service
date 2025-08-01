namespace AccountService.Features.Accounts;

/// <summary>
/// Объект для передачи данных о счёте клиенту (DTO - Data Transfer Object).
/// </summary>
public class AccountDto
{
    /// <summary>
    /// Уникальный идентификатор счёта (GUID).
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор владельца счёта (клиента).
    /// </summary>
    /// <example>a1b2c3d4-e5f6-7890-1234-567890abcdef</example>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Тип счёта. Возможные значения: Checking, Deposit, Credit.
    /// </summary>
    /// <example>Deposit</example>
    public required string AccountType { get; set; }

    /// <summary>
    /// Код валюты в формате ISO 4217.
    /// </summary>
    /// <example>RUB</example>
    public required string Currency { get; set; }

    /// <summary>
    /// Текущий баланс счёта.
    /// </summary>
    /// <example>150750.25</example>
    public decimal Balance { get; set; }

    /// <summary>
    /// Процентная ставка по счёту. Может быть null, если не применима к данному типу счёта (например, для текущего счёта).
    /// </summary>
    /// <example>4.5</example>
    public decimal? InterestRate { get; set; }

    /// <summary>
    /// Дата и время открытия счёта (в формате UTC).
    /// </summary>
    /// <example>2024-05-20T10:30:00Z</example>
    public DateTime OpenedDate { get; set; }
    
    /// <summary>
    /// Дата и время закрытия счёта (в формате UTC). Будет null, если счёт активен.
    /// </summary>
    /// <example>null</example>
    public DateTime? ClosedDate { get; set; } // Добавил поле ClosedDate, т.к. оно есть в задании
};