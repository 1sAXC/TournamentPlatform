using Microsoft.EntityFrameworkCore;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Repositories;

public sealed class UserProjectionRepository(TournamentDbContext dbContext) : IUserProjectionRepository
{
    public Task<UserProjection?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.UserProjections.FirstOrDefaultAsync(
            projection => projection.UserId == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserProjection>> GetByIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        return await dbContext.UserProjections
            .Where(projection => userIds.Contains(projection.UserId))
            .ToArrayAsync(cancellationToken);
    }

    public void Add(UserProjection projection)
    {
        dbContext.UserProjections.Add(projection);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
