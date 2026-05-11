namespace TournamentPlatform.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string Username { get; init; } = "guest";
    public string Password { get; init; } = "guest";
    public string VirtualHost { get; init; } = "/";
    public string ExchangeName { get; init; } = "tournament-platform.events";
    public string DeadLetterExchangeName { get; init; } = "tournament-platform.events.dead-letter";
    public int PublishRetryCount { get; init; } = 3;
    public int PublishRetryDelayMilliseconds { get; init; } = 1000;
    public int OutboxBatchSize { get; init; } = 50;
    public int OutboxPollingIntervalMilliseconds { get; init; } = 5000;
}
