using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Dto;
using TournamentPlatform.Shared.Common;

namespace Tournament.Application.Matches;

public interface IMatchResultService
{
    Task<Result<TournamentDetailsResponse>> CompleteMatchAsync(
        Guid tournamentId,
        Guid matchId,
        CompleteMatchRequest request,
        CurrentTournamentUser currentUser,
        CancellationToken cancellationToken = default);
}
