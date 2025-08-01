using System.Text.Json.Serialization;
using AccountService.Core.Domain;
using MediatR;

namespace AccountService.Features.Transactions.GetAccountStatement;

public class GetAccountStatementQuery : IRequest<AccountStatementDto>
{
    [JsonIgnore] // Из URL
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