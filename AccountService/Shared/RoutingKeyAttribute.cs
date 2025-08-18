namespace AccountService.Shared;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RoutingKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}