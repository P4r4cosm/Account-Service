namespace BankAccounts.Features.Accounts;

/// <summary>
/// Объект для передачи данных о счёте клиенту.
/// </summary>
public record AccountDto(
    Guid Id,
    Guid OwnerId,
    string AccountType,
    string Currency, // <-- Вы были абсолютно правы, он здесь нужен!
    decimal Balance,
    decimal? InterestRate, // <-- Тоже полезно вернуть
    DateTime OpenedDate
);