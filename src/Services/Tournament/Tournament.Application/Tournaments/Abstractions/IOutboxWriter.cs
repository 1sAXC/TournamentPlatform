using TournamentPlatform.Contracts.Events;

namespace Tournament.Application.Tournaments.Abstractions;

public interface IOutboxWriter
{
    void Add(IntegrationEvent integrationEvent);
}
