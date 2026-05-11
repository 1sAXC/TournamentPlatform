namespace Tournament.Application.Tournaments.Services;

public interface ITournamentLifecycleService
{
    Task TryStartTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default);
}
