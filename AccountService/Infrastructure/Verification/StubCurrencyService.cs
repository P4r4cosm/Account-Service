namespace AccountService.Infrastructure.Verification;


/// <summary>
/// Заглушка, имитирующая работу внешнего сервиса валют.
/// </summary>
public class StubCurrencyService: ICurrencyService
{
    // Набор поддерживаемых валют
    private static readonly HashSet<string> SupportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "RUB",
        "USD",
        "EUR"
    };

    
    public Task<bool> IsSupportedAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        var isSupported = !string.IsNullOrEmpty(currencyCode) && SupportedCurrencies.Contains(currencyCode);
        return Task.FromResult(isSupported);
    }
}