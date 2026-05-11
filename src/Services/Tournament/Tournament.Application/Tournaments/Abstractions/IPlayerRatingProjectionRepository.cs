using Tournament.Domain.Tournaments;

namespace Tournament.Application.Tournaments.Abstractions;

public interface IPlayerRatingProjectionRepository
{
    Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdsAsync(IReadOnlyCollection<Guid> playerIds, string disciplineCode, CancellationToken cancellationToken = default);
    Task<PlayerRatingProjection?> GetAsync(Guid playerId, string disciplineCode, CancellationToken cancellationToken = default);
    Task<bool> DeletedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddDeletedUserAsync(Guid userId, DateTime deletedAtUtc, CancellationToken cancellationToken = default);
    void Add(PlayerRatingProjection projection);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
