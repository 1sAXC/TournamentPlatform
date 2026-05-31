using Tournament.Domain.Tournaments;
using Tournament.Application.Tournaments.Dto;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Tournaments.Abstractions;

public interface ITournamentRepository
{
    Task<bool> TitleExistsAsync(string normalizedTitle, CancellationToken cancellationToken = default);
    Task<Discipline?> GetActiveDisciplineAsync(string disciplineCode, CancellationToken cancellationToken = default);
    Task<Tournament.Domain.Tournaments.Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentSummaryDto>> GetByStatusAsync(TournamentStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentSummaryDto>> GetAvailableAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentSummaryDto>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TournamentSummaryDto>> GetByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default);
    Task<bool> BlockedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ITournamentTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    void Add(Tournament.Domain.Tournaments.Tournament tournament);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
