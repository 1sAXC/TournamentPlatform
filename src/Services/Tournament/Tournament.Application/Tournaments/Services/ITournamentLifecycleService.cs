namespace Tournament.Application.Tournaments.Services;

public interface ITournamentLifecycleService
{
    Task TryStartTournamentAsync(Domain.Tournaments.Tournament tournament, CancellationToken cancellationToken = default);
}
