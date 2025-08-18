using System.Reflection;
using AccountService.Shared.Events;

namespace AccountService.Shared.Behaviors;

public class EventRoutingKeyMapper : IEventRoutingKeyMapper
{
    private readonly Dictionary<string, string> _routingKeys;

    public EventRoutingKeyMapper()
    {
        // получаем сборку в которой находятся события
        var eventsAssembly = typeof(AccountOpenedEvent).Assembly;

        _routingKeys = eventsAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetCustomAttribute<RoutingKeyAttribute>() != null)
            .ToDictionary(
                t => t.Name, // Ключ словаря - имя класса, например "AccountOpenedEvent"
                t => t.GetCustomAttribute<RoutingKeyAttribute>()!.Key // Значение - ключ из атрибута
            );
    }

    public string GetRoutingKey(string eventType)
    {
        return _routingKeys.TryGetValue(eventType, out var key) ? key :
            // Если для какого-то типа события не нашелся ключ
            "unknown";
    }
}