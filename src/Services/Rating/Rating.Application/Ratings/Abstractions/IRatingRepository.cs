using Rating.Domain.Ratings;

namespace Rating.Application.Ratings.Abstractions;

public interface IRatingRepository
{
    Task<IReadOnlyCollection<PlayerRating>> GetPlayerRatingsAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<PlayerRating?> GetPlayerRatingAsync(Guid playerId, string disciplineCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RatingHistory>> GetPlayerHistoryAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyRatingAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<bool> HasMatchHistoryAsync(Guid matchId, CancellationToken cancellationToken = default);
    void AddPlayerRating(PlayerRating rating);
    void AddRatingHistory(RatingHistory history);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
