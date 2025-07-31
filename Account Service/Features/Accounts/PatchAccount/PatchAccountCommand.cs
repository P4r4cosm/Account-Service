
using MediatR;
using System.Text.Json.Serialization;

namespace AccountService.Features.Accounts.PatchAccount;

public class PatchAccountCommand : IRequest<Unit>
{
    // ID из маршрута, не из тела запроса
    [JsonIgnore] 
    public Guid AccountId { get; set; }

    // Nullable, так как поле может отсутствовать в запросе
    public Guid? OwnerId { get; set; } 

    public decimal? InterestRate { get; set; }
    
    // Сюда можно добавить другие изменяемые поля, например, для закрытия счёта
    public DateTime? CloseDate { get; set; }
}