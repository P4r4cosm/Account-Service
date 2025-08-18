using System.Text.Json.Serialization;

namespace AccountService.Shared.Events;

/// <summary>
/// Событие, потребляемое из очереди `account.antifraud`,
/// сигнализирующее о необходимости заблокировать все расходные операции для клиента.
/// Routing Key: `client.blocked`
/// </summary>
public class ClientBlockedEvent
{
    /// <summary>
    /// Уникальный идентификатор клиента, чьи счета необходимо заблокировать.
    /// </summary>
    [JsonPropertyName("clientId")]
    public Guid ClientId { get; init; }
}