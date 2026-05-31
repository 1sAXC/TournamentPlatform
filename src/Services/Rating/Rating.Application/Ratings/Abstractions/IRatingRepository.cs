using Rating.Domain.Ratings;

namespace Rating.Application.Ratings.Abstractions;

public interface IRatingRepository
{
    Task<IReadOnlyCollection<PlayerRating>> GetPlayerRatingsAsync(Guid playerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns all rating rows for the player including those soft-deleted via
    /// <see cref="PlayerRating.MarkDeleted"/>. Used by the unblock/restore
    /// path; for normal reads use <see cref="GetPlayerRatingsAsync"/>.
    /// </summary>
    Task<IReadOnlyCollection<PlayerRating>> GetAllPlayerRatingsAsync(Guid playerId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Loads a single rating row INCLUDING soft-deleted rows. Used by the
    /// match-completion path so a blocked player who happens to be in a
    /// match doesn't trigger a duplicate-key insert against the unique
    /// (PlayerId, DisciplineCode) index.
    /// </summary>
    Task<PlayerRating?> GetPlayerRatingIncludingDeletedAsync(Guid playerId, string disciplineCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RatingHistory>> GetPlayerHistoryAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<bool> HasAnyRatingAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<bool> HasMatchHistoryAsync(Guid matchId, CancellationToken cancellationToken = default);
    void AddPlayerRating(PlayerRating rating);
    void AddRatingHistory(RatingHistory history);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
