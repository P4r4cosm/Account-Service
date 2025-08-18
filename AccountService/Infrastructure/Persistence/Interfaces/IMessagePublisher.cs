using AccountService.Shared.Domain;

namespace AccountService.Infrastructure.Persistence.Interfaces;

public interface IMessagePublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}