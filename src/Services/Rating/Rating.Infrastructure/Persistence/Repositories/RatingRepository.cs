using Microsoft.EntityFrameworkCore;
using Rating.Application.Ratings.Abstractions;
using Rating.Domain.Ratings;

namespace Rating.Infrastructure.Persistence.Repositories;

public sealed class RatingRepository(RatingDbContext dbContext) : IRatingRepository
{
    public async Task<IReadOnlyCollection<PlayerRating>> GetPlayerRatingsAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlayerRatings
            .Where(rating => rating.PlayerId == playerId && !rating.IsDeleted)
            .OrderBy(rating => rating.DisciplineCode)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlayerRating>> GetAllPlayerRatingsAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlayerRatings
            .Where(rating => rating.PlayerId == playerId)
            .OrderBy(rating => rating.DisciplineCode)
            .ToArrayAsync(cancellationToken);
    }

    public Task<PlayerRating?> GetPlayerRatingAsync(
        Guid playerId,
        string disciplineCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedDisciplineCode = disciplineCode.Trim();
        return dbContext.PlayerRatings.FirstOrDefaultAsync(
            rating => rating.PlayerId == playerId
                && rating.DisciplineCode == normalizedDisciplineCode
                && !rating.IsDeleted,
            cancellationToken);
    }

    public Task<PlayerRating?> GetPlayerRatingIncludingDeletedAsync(
        Guid playerId,
        string disciplineCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedDisciplineCode = disciplineCode.Trim();
        return dbContext.PlayerRatings.FirstOrDefaultAsync(
            rating => rating.PlayerId == playerId
                && rating.DisciplineCode == normalizedDisciplineCode,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<RatingHistory>> GetPlayerHistoryAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.RatingHistories
            .Where(history => history.PlayerId == playerId)
            .OrderByDescending(history => history.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> HasAnyRatingAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        return dbContext.PlayerRatings.AnyAsync(rating => rating.PlayerId == playerId, cancellationToken);
    }

    public Task<bool> HasMatchHistoryAsync(Guid matchId, CancellationToken cancellationToken = default)
    {
        return dbContext.RatingHistories.AnyAsync(history => history.MatchId == matchId, cancellationToken);
    }

    public void AddPlayerRating(PlayerRating rating)
    {
        dbContext.PlayerRatings.Add(rating);
    }

    public void AddRatingHistory(RatingHistory history)
    {
        dbContext.RatingHistories.Add(history);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
