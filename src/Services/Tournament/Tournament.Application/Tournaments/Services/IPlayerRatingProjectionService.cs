using TournamentPlatform.Contracts.Events;

namespace Tournament.Application.Tournaments.Services;

public interface IPlayerRatingProjectionService
{
    Task HandleUserCreatedAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default);
    Task HandleRatingUpdatedAsync(RatingUpdatedEvent integrationEvent, CancellationToken cancellationToken = default);
    Task HandleUserDeletedAsync(UserDeletedEvent integrationEvent, CancellationToken cancellationToken = default);
}
