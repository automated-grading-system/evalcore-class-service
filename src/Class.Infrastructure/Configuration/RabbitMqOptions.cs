using Microsoft.Extensions.Configuration;

namespace Class.Infrastructure.Configuration;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string Username { get; init; } = "ags";

    public string Password { get; init; } = "ags_password";

    public string Exchange { get; init; } = "ags.domain.events";

    public static RabbitMqOptions FromConfiguration(IConfiguration configuration)
    {
        return new RabbitMqOptions
        {
            Host = Read(configuration, "RABBITMQ_HOST", "RabbitMq:Host", "localhost"),
            Port = int.TryParse(Read(configuration, "RABBITMQ_PORT", "RabbitMq:Port", "5672"), out var port) ? port : 5672,
            Username = Read(configuration, "RABBITMQ_USERNAME", "RabbitMq:Username", "ags"),
            Password = Read(configuration, "RABBITMQ_PASSWORD", "RabbitMq:Password", "ags_password"),
            Exchange = Read(configuration, "RABBITMQ_EXCHANGE", "RabbitMq:Exchange", "ags.domain.events")
        };
    }

    private static string Read(IConfiguration configuration, string environmentKey, string configKey, string defaultValue)
    {
        return configuration[environmentKey] ?? configuration[configKey] ?? defaultValue;
    }
}
