using Tournament.Application.Tournaments;
using TournamentPlatform.Shared.Common;

namespace Tournament.Application.Brackets;

public interface ISwissRoundService
{
    Task<Result> CreateNextRoundAsync(
        Guid tournamentId,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default);
}
