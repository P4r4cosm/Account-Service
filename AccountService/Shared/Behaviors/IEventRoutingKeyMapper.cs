namespace AccountService.Shared.Behaviors;

public interface IEventRoutingKeyMapper
{
    string GetRoutingKey(string eventType);
}