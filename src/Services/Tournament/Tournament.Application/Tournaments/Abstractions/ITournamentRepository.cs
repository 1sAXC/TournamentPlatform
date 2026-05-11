using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Tournaments.Abstractions;

public interface ITournamentRepository
{
    Task<bool> TitleExistsAsync(string normalizedTitle, CancellationToken cancellationToken = default);
    Task<Discipline?> GetActiveDisciplineAsync(string disciplineCode, CancellationToken cancellationToken = default);
    Task<Tournament.Domain.Tournaments.Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Tournament.Domain.Tournaments.Tournament>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Tournament.Domain.Tournaments.Tournament>> GetByStatusAsync(TournamentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Tournament.Domain.Tournaments.Tournament>> GetAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Tournament.Domain.Tournaments.Tournament>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Tournament.Domain.Tournaments.Tournament>> GetByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<bool> DeletedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ITournamentTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    void Add(Tournament.Domain.Tournaments.Tournament tournament);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
