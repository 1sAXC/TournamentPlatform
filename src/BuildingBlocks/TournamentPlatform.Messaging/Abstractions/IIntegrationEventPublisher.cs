using TournamentPlatform.Contracts.Events;

namespace TournamentPlatform.Messaging.Abstractions;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent;

    Task PublishRawAsync(
        Guid eventId,
        string eventType,
        string payload,
        string? correlationId = null,
        CancellationToken cancellationToken = default);
}
