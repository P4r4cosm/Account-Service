using System.Text.Json;
using AccountService.Infrastructure.Persistence.Interfaces;
using AccountService.Shared.Domain;
using AccountService.Shared.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace AccountService.Features.TestStub;

/// <summary>
/// Контроллер для тестирование AntifraudConsumer (удобной публикации событий client.blocked/unblocked).
/// </summary>
[ApiController]
[Route("api/testing-stubs/clients/{clientId:guid}")]
[ApiExplorerSettings(GroupName = "v1")] 
[Authorize]

public class TestingStubsController(IMessagePublisher publisher, ILogger<TestingStubsController> logger)
    : ControllerBase
{
    /// <summary>
    /// [ТОЛЬКО ДЛЯ ТЕСТИРОВАНИЯ] Публикует событие ClientBlocked для имитации ответа от сервиса Antifraud.
    /// </summary>
    /// <param name="clientId">ID клиента, счета которого нужно заблокировать.</param>
    /// <returns>Статус операции.</returns>
    [HttpPost("block")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> BlockClient(Guid clientId)
    {
        logger.LogWarning("Вызван тестовый эндпоинт для блокировки клиента {ClientId}", clientId);
        var clientBlockedEvent = new ClientBlockedEvent
        {
            ClientId = clientId
        };
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var eventEnvelope = new EventEnvelope<ClientBlockedEvent>(clientBlockedEvent, correlationId, causationId);
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(ClientBlockedEvent),
            Payload = JsonSerializer.Serialize(eventEnvelope),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = correlationId
        };
        await publisher.PublishAsync(outboxMessage);
        return Accepted(new { message = $"Запрос на блокировку счетов клиента {clientId} отправлен." });
    }

    /// <summary>
    /// [ТОЛЬКО ДЛЯ ТЕСТИРОВАНИЯ] Публикует событие ClientUnblocked для снятия блокировки.
    /// </summary>
    /// <param name="clientId">ID клиента, счета которого нужно разблокировать.</param>
    /// <returns>Статус операции.</returns>
    [HttpPost("unblock")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> UnblockClient(Guid clientId)
    {
        logger.LogWarning("Вызван тестовый эндпоинт для разблокировки клиента {ClientId}", clientId);
        var clientUnblockedEvent = new ClientUnblockedEvent
        {
            ClientId = clientId
        };
        var correlationId = Guid.NewGuid();
        var causationId = Guid.NewGuid();
        var eventEnvelope = new EventEnvelope<ClientUnblockedEvent>(clientUnblockedEvent, correlationId, causationId);
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(ClientUnblockedEvent),
            Payload = JsonSerializer.Serialize(eventEnvelope),
            OccurredAt = DateTime.UtcNow,
            CorrelationId = correlationId
        };
        await publisher.PublishAsync(outboxMessage);
        return Accepted(new { message = $"Запрос на разблокировку счетов клиента {clientId} отправлен." });
    }
}