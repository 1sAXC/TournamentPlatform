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

    public void Add(UserProjection projection)
    {
        dbContext.UserProjections.Add(projection);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
