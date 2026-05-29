using Tournament.Domain.Tournaments;

namespace Tournament.Application.Tournaments.Abstractions;

public interface IUserProjectionRepository
{
    Task<UserProjection?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<UserProjection>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
    void Add(UserProjection projection);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
