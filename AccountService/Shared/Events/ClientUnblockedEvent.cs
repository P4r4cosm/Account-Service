using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;
/// <summary>
/// Событие, потребляемое из очереди `account.antifraud`,
/// сигнализирующее о необходимости снять блокировку расходных операций для клиента.
/// Routing Key: `client.unblocked`
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]  // ReSharper предупреждает об отсутствии использования, но свойства нужны для сериализации через System.Text.Json.
// ReSharper disable once ClassNeverInstantiated.Global 
public class ClientUnblockedEvent
{
    /// <summary>
    /// Уникальный идентификатор клиента, чьи счета необходимо разблокировать.
    /// </summary>
    [JsonPropertyName("clientId")]
    public Guid ClientId { get; set; }
}