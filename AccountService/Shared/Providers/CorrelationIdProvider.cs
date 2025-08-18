namespace AccountService.Shared.Providers;

public class CorrelationIdProvider(IHttpContextAccessor httpContextAccessor) : ICorrelationIdProvider
{
    public Guid GetCorrelationId()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is not null &&
            httpContext.Items.TryGetValue("CorrelationId", out var correlationIdObj) &&
            Guid.TryParse(correlationIdObj?.ToString(), out var correlationId))
        {
            return correlationId;
        }

        // Возвращаем новый Guid, если по какой-то причине ID не был установлен
        // (например, при выполнении фоновой задачи вне HTTP-контекста).
        return Guid.NewGuid();
    }
}