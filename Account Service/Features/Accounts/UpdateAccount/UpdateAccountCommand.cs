using System.Text.Json.Serialization;
using MediatR;

namespace AccountService.Features.Accounts.UpdateAccount;

/// <summary>
/// Команда для полного обновления данных банковского счёта.
/// </summary>
public class UpdateAccountCommand : IRequest<Unit>
{
    /// <summary>
    /// ID счёта, который нужно обновить. Передается через URL.
    /// </summary>
    [JsonIgnore] // Это свойство не должно приходить из тела запроса
    public Guid AccountId { get; set; }

    /// <summary>
    /// Новый ID владельца счёта.
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid OwnerId { get; set; }

    /// <summary>
    /// Новая процентная ставка (если применимо к типу счёта).
    /// Для счетов типа "Checking" это поле игнорируется.
    /// </summary>
    /// <example>5.5</example>
    public decimal? InterestRate { get; set; }
}