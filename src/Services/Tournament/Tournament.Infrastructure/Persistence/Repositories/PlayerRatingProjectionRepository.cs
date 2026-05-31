using Microsoft.EntityFrameworkCore;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Repositories;

public sealed class PlayerRatingProjectionRepository(TournamentDbContext dbContext)
    : IPlayerRatingProjectionRepository
{
    public async Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlayerRatingProjections
            .Where(projection => projection.PlayerId == playerId)
            .ToArrayAsync(cancellationToken);
    }

    public Task<PlayerRatingProjection?> GetAsync(
        Guid playerId,
        string disciplineCode,
        CancellationToken cancellationToken = default)
    {
        return dbContext.PlayerRatingProjections.FirstOrDefaultAsync(
            projection => projection.PlayerId == playerId
                && projection.DisciplineCode == disciplineCode,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdsAsync(
        IReadOnlyCollection<Guid> playerIds,
        string disciplineCode,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PlayerRatingProjections
            .Where(projection => playerIds.Contains(projection.PlayerId)
                && projection.DisciplineCode == disciplineCode)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> BlockedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.BlockedUserProjections.AnyAsync(
            projection => projection.UserId == userId,
            cancellationToken);
    }

    public async Task AddBlockedUserAsync(
        Guid userId,
        DateTime blockedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await dbContext.BlockedUserProjections.AddAsync(
            BlockedUserProjection.Create(userId, blockedAtUtc),
            cancellationToken);
    }

    public async Task RemoveBlockedUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await dbContext.BlockedUserProjections
            .FirstOrDefaultAsync(projection => projection.UserId == userId, cancellationToken);
        if (existing is not null)
        {
            dbContext.BlockedUserProjections.Remove(existing);
        }
    }

    public void Add(PlayerRatingProjection projection)
    {
        dbContext.PlayerRatingProjections.Add(projection);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
