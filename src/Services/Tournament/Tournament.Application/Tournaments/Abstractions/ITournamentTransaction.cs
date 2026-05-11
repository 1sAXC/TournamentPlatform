namespace Tournament.Application.Tournaments.Abstractions;

public interface ITournamentTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
