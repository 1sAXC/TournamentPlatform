using Rating.Application.Ratings.Dto;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Shared.Common;

namespace Rating.Application.Ratings.Services;

public interface IRatingService
{
    Task HandleUserCreatedAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default);
    Task HandleUserDeletedAsync(UserDeletedEvent integrationEvent, CancellationToken cancellationToken = default);
    Task HandleMatchCompletedAsync(MatchCompletedEvent integrationEvent, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<PlayerRatingResponse>>> GetPlayerRatingsAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<Result<PlayerRatingResponse>> GetPlayerRatingAsync(Guid playerId, string disciplineCode, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<RatingHistoryResponse>>> GetPlayerHistoryAsync(Guid playerId, CancellationToken cancellationToken = default);
}
