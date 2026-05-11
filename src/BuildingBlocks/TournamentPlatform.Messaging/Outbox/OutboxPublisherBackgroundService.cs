using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TournamentPlatform.Messaging.Abstractions;
using TournamentPlatform.Messaging.RabbitMq;

namespace TournamentPlatform.Messaging.Outbox;

public sealed class OutboxPublisherBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<OutboxPublisherBackgroundService> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PublishBatchAsync(stoppingToken);
            await Task.Delay(_options.OutboxPollingIntervalMilliseconds, stoppingToken);
        }
    }

    private async Task PublishBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<IOutboxMessageStore>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var messages = await store.GetUnprocessedAsync(_options.OutboxBatchSize, cancellationToken);
        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishRawAsync(
                    message.EventId,
                    message.EventType,
                    message.Payload,
                    cancellationToken: cancellationToken);

                await store.MarkProcessedAsync(message.Id, DateTime.UtcNow, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Outbox publish failed for {EventType} {EventId}",
                    message.EventType,
                    message.EventId);

                await store.MarkFailedAsync(message.Id, exception.Message, cancellationToken);
            }
        }
    }
}
