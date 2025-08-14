namespace AccountService.Infrastructure.Verification;
/// <summary>
/// Заглушка, имитирующая работу внешнего сервиса верификации клиентов.
///</summary>
public class StubClientVerificationService : IClientVerificationService
{
    // В реальном приложении здесь был бы HTTP-запрос к микросервису "User Service".
    // В нашей заглушке мы просто считаем, что любой непустой GUID является валидным.
    public Task<bool> ClientExistsAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        // Простая имитация: считаем, что клиент существует, если его ID не пустой.
        var exists = ownerId != Guid.Empty;
        return Task.FromResult(exists);
    }
}