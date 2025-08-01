using System.Diagnostics.CodeAnalysis;

namespace AccountService.Infrastructure.Verification;

/// <summary>
/// Определяет контракт для сервиса, проверяющего валюты.
/// </summary>
public interface ICurrencyService
{
    /// <summary>
    /// Проверяет, поддерживается ли указанная валюта системой.
    /// </summary>
    /// <param name="currencyCode">Трехбуквенный код валюты (ISO 4217).</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>True, если валюта поддерживается, иначе false.</returns>
    [SuppressMessage("ReSharper", "UnusedParameter.Global")] //Resharper жалуется на неиспользование токена в реализациях, но он добавлен на будущее
    
    Task<bool> IsSupportedAsync(string currencyCode, CancellationToken cancellationToken);
}