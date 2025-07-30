namespace AccountService.Infrastructure.Verification;

/// <summary>
/// Определяет контракт для сервиса, проверяющего существование клиентов.
/// </summary>
public interface IClientVerificationService
{
    /// <summary>
    /// Проверяет, существует ли клиент с указанным ID.
    /// </summary>
    /// <param name="ownerId">ID клиента для проверки.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>True, если клиент существует, иначе false.</returns>
    Task<bool> ClientExistsAsync(Guid ownerId, CancellationToken cancellationToken = default);
}