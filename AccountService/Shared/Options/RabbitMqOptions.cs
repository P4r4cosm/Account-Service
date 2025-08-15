namespace AccountService.Shared.Options;

public class RabbitMqOptions
{
    public string HostName { get; init; } = null!;
    public int Port { get; init; }
    public string UserName { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string VirtualHost { get; init; } = null!;
}