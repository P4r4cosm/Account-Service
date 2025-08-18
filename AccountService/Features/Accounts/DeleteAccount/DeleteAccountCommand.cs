using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using AccountService.Shared.Domain;
using MediatR;

namespace AccountService.Features.Accounts.DeleteAccount;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")] //Resharper решает, что set-еры не нужны, а они нужны для корректного создания команд в эндпоинтах
public class DeleteAccountCommand : IRequest<MbResult>
{
    /// <summary>
    /// ID комманды
    /// </summary>
    [JsonIgnore]
    public Guid CommandId { get; set; }
    public Guid AccountId{get;set;}
    
    /// <summary>
    /// Необязательный идентификатор корреляции для сквозной трассировки.
    /// Если не предоставлен клиентом, будет сгенерирован автоматически.
    /// Это свойство не биндится из тела запроса, а устанавливается в контроллере.
    /// </summary>
    [JsonIgnore]
    public Guid? CorrelationId { get; set; }
    
}
