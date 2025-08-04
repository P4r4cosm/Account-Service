
using System.Diagnostics.CodeAnalysis;
using MediatR;
using System.Text.Json.Serialization;
using AccountService.Shared.Domain;

namespace AccountService.Features.Accounts.PatchAccount;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] //Resharper решает, что set-еры не нужны, а они нужны для корректного создания команд в эндпоинтах
public class PatchAccountCommand : IRequest<MbResult>
{
    
    /// <summary>
    /// ID счёта, который необходимо обновить.
    /// </summary>
    /// <remarks>
    /// Это свойство не передаётся в теле запроса, а берётся из URL (`/api/accounts/{accountId}`).
    /// </remarks>
    // ID из маршрута, не из тела запроса
    [JsonIgnore] 
    public Guid AccountId { get; set; }

    // Nullable, так как поле может отсутствовать в запросе
    /// <summary>
    /// Новый ID владельца счёта.
    /// </summary>
    /// <remarks>
    /// Это поле является обязательным для `PUT` запроса.
    /// Указанный клиент должен существовать в системе.
    /// </remarks>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? OwnerId { get; set; } 
    /// <summary>
    /// Новая процентная ставка.
    /// </summary>
    /// <remarks>
    /// - Для счетов типа `Deposit` или `Credit` это поле устанавливает новую годовую ставку.
    /// - Для счетов типа `Checking` это поле должно быть `null`, иначе запрос вернёт ошибку.
    /// </remarks>
    /// <example>5.5</example>
    public decimal? InterestRate { get; set; }
    /// <summary>
    /// Дата закрытия счёта.
    /// </summary>
    /// <remarks>
    /// - Установка даты означает закрытие счёта. Это возможно только для счетов с нулевым балансом.
    /// - Дата закрытия не может быть раньше даты открытия.
    /// - Чтобы "активировать" ранее закрытый счёт, передайте `null` в этом поле.
    /// </remarks>
    /// <example>2025-12-31T10:00:00Z</example>
    public DateTime? CloseDate { get; set; }
}