using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Messaging.Abstractions;

namespace TournamentPlatform.Messaging.RabbitMq;

public sealed class RabbitMqIntegrationEventPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions _options = options.Value;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonSerializerOptions);
        return PublishRawAsync(
            integrationEvent.EventId,
            integrationEvent.EventType,
            payload,
            integrationEvent.CorrelationId,
            cancellationToken);
    }

    public async Task PublishRawAsync(
        Guid eventId,
        string eventType,
        string payload,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.PublishRetryCount; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                PublishCore(eventId, eventType, payload, correlationId);
                return;
            }
            catch (Exception exception) when (attempt < _options.PublishRetryCount)
            {
                lastException = exception;
                logger.LogWarning(
                    exception,
                    "RabbitMQ publish failed for {EventType} {EventId}. Attempt {Attempt}/{RetryCount}",
                    eventType,
                    eventId,
                    attempt,
                    _options.PublishRetryCount);

                await Task.Delay(_options.PublishRetryDelayMilliseconds, cancellationToken);
            }
        }

        throw lastException ?? new InvalidOperationException($"RabbitMQ publish failed for {eventType} {eventId}.");
    }

    private void PublishCore(Guid eventId, string eventType, string payload, string? correlationId)
    {
        var factory = CreateConnectionFactory();
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.Type = eventType;
        properties.MessageId = eventId.ToString();
        properties.CorrelationId = correlationId;

        var body = Encoding.UTF8.GetBytes(payload);
        channel.BasicPublish(
            _options.ExchangeName,
            routingKey: eventType,
            mandatory: false,
            basicProperties: properties,
            body: body);
    }

    private ConnectionFactory CreateConnectionFactory()
    {
        return new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.Username,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true
        };
    }
}
