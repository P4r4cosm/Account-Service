
using System.Diagnostics.CodeAnalysis;
using MediatR;
using System.Text.Json.Serialization;
using AccountService.Shared.Domain;

namespace AccountService.Features.Accounts.PatchAccount;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] //Resharper решает, что set-еры не нужны, а они нужны для корректного создания команд в эндпоинтах
public class PatchAccountCommand : IRequest<MbResult>
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