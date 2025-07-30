namespace AccountService.Infrastructure.Verification;


/// <summary>
/// Заглушка, имитирующая работу внешнего сервиса валют.
/// </summary>
public class StubCurrencyService: ICurrencyService
{
    // Набор поддерживаемых валют
    private static readonly HashSet<string> _supportedCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "RUB",
        "USD",
        "EUR"
    };

    // В реальном приложении здесь мог бы быть запрос к API Центробанка или к внутренней базе.
    public Task<bool> IsSupportedAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        bool isSupported = !string.IsNullOrEmpty(currencyCode) && _supportedCurrencies.Contains(currencyCode);
        return Task.FromResult(isSupported);
    }
}