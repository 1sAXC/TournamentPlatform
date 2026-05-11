using TournamentPlatform.Contracts.Events;

namespace TournamentPlatform.Messaging.Abstractions;

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}
