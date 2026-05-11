using TournamentPlatform.Contracts.Events;

namespace Rating.Application.Ratings.Abstractions;

public interface IOutboxWriter
{
    void Add(IntegrationEvent integrationEvent);
}
