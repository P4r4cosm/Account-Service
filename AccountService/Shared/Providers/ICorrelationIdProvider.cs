namespace AccountService.Shared.Providers;

public interface ICorrelationIdProvider
{
    Guid GetCorrelationId();
}