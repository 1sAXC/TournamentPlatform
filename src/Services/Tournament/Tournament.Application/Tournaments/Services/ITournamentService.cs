using Tournament.Application.Tournaments.Dto;
using TournamentPlatform.Shared.Common;

namespace Tournament.Application.Tournaments.Services;

public interface ITournamentService
{
    Task<Result<TournamentDetailsResponse>> CreateAsync(
        CreateTournamentRequest request,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default);

    Task<Result<TournamentDetailsResponse>> CreateByAdminAsync(
        AdminCreateTournamentRequest request,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<TournamentDetailsResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetAvailableAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetCompletedAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetMyAsync(CurrentTournamentUser currentUser, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyCollection<TournamentListItemResponse>>> GetOrganizerTournamentsAsync(CurrentTournamentUser currentUser, CancellationToken cancellationToken = default);
    Task<Result<TournamentDetailsResponse>> RegisterPlayerAsync(Guid tournamentId, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default);
    Task<Result<TournamentDetailsResponse>> LeaveAsync(Guid tournamentId, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default);
    Task<Result<TournamentDetailsResponse>> UpdateAsync(Guid tournamentId, UpdateTournamentRequest request, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default);
    Task<Result<TournamentDetailsResponse>> CancelAsync(Guid tournamentId, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid tournamentId, CurrentTournamentUser currentUser, CancellationToken cancellationToken = default);
}
