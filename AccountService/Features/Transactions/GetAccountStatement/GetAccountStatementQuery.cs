using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AccountService.Shared.Domain;
using MediatR;
using Swashbuckle.AspNetCore.Annotations;

namespace AccountService.Features.Transactions.GetAccountStatement;


/// <summary>
/// Команда для получения выписки по счёту.
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")] //Resharper решает, что set-еры не нужны, а они нужны для корректного создания команд в эндпоинтах
public class GetAccountStatementQuery : IRequest<MbResult<AccountStatementDto>>
{

    [JsonIgnore]
    [SwaggerSchema(ReadOnly = true)]
    public Guid AccountId { get; set; }

    /// <summary>
    /// Начальная дата периода для фильтрации (включительно).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Конечная дата периода для фильтрации (включительно).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Номер страницы.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Размер страницы.
    /// </summary>
    public int PageSize { get; set; } = 20;
}