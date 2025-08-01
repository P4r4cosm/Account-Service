using System.Diagnostics.CodeAnalysis;
using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Accounts.GetAccounts;

/// <summary>
/// Запрос на получение списка счетов с расширенными параметрами фильтрации.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")] //Resharper решает, что set-еры не нужны, а они нужны для корректного создания команд в эндпоинтах
public class GetAccountsQuery : IRequest<PagedResult<AccountDto>>
{
    /// <summary>
    /// Фильтр по ID владельца счёта.
    /// </summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid? OwnerId { get; set; }

    /// <summary>
    /// Фильтр по типу счёта.
    /// </summary>
    /// <example>Deposit</example>
    public string? AccountType { get; set; }

    /// <summary>
    /// Фильтр по коду валюты (ISO 4217).
    /// </summary>
    /// <example>RUB</example>
    public string? Currency { get; set; }

    /// <summary>
    /// Фильтр по минимальному балансу (включительно).
    /// </summary>
    /// <example>5000</example>
    public decimal? BalanceGte { get; set; } // gte = Greater Than or Equal

    /// <summary>
    /// Фильтр по максимальному балансу (включительно).
    /// </summary>
    /// <example>100000</example>
    public decimal? BalanceLte { get; set; } // lte = Less Than or Equal
        
    /// <summary>
    /// Фильтр по начальной дате открытия счёта (включительно).
    /// </summary>
    /// <example>2025-01-01</example>
    public DateTime? OpeningDateFrom { get; set; }

    /// <summary>
    /// Фильтр по конечной дате открытия счёта (включительно).
    /// </summary>
    /// <example>2025-07-29</example>
    public DateTime? OpeningDateTo { get; set; }
    
    /// <summary>
    /// Номер страницы (начиная с 1).
    /// </summary>
    /// <example>1</example>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Размер страницы (количество элементов).
    /// </summary>
    /// <example>20</example>
    public int PageSize { get; set; } = 10; // Значение по умолчанию
}