namespace AccountService.Shared.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        // Пытаемся получить CorrelationId из заголовка
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();

        if (string.IsNullOrEmpty(correlationId))
        {
            // Если его нет, генерируем новый
            correlationId = Guid.NewGuid().ToString();
        }

        // Сохраняем CorrelationId в специальной коллекции HttpContext.Items.
        // Это безопасный способ передавать данные внутри одного запроса.
        context.Items["CorrelationId"] = correlationId;
        
        // Добавляем CorrelationId в заголовок ответа, чтобы клиент мог его видеть
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeaderName] = new[] { correlationId };
            return Task.CompletedTask;
        });

        await next(context);
    }
}