using Tournament.Domain.Tournaments;

namespace Tournament.Application.Brackets;

public interface IBracketGenerator
{
    Task GenerateInitialAsync(
        Domain.Tournaments.Tournament tournament,
        IReadOnlyList<Team> teams,
        CancellationToken ct);

    Task HandleMatchCompletedAsync(
        Domain.Tournaments.Tournament tournament,
        Match completedMatch,
        CancellationToken ct);
}
