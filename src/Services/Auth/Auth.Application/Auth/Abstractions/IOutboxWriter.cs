using TournamentPlatform.Contracts.Events;

namespace Auth.Application.Auth.Abstractions;

public interface IOutboxWriter
{
    void Add(IntegrationEvent integrationEvent);
}
