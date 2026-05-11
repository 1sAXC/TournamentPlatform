using TournamentPlatform.Contracts.Events;

namespace TournamentPlatform.Messaging.Abstractions;

public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IntegrationEvent
{
    Task ConsumeAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
