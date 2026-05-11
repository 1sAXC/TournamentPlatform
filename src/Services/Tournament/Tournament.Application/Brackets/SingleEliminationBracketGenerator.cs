using Tournament.Application.Tournaments.Abstractions;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Brackets;

public sealed class SingleEliminationBracketGenerator(IOutboxWriter outboxWriter)
    : BracketGeneratorBase(outboxWriter)
{
    protected override BracketType BracketType => BracketType.Main;

    public override Task HandleMatchCompletedAsync(
        Domain.Tournaments.Tournament tournament,
        Domain.Tournaments.Match completedMatch,
        CancellationToken ct)
    {
        var round = FindRound(tournament, completedMatch);
        if (round is null || round.Matches.Any(match => match.Status != MatchStatus.Completed))
        {
            return Task.CompletedTask;
        }

        round.Complete(DateTime.UtcNow);
        var winners = round.Matches
            .Select(match => match.WinnerTeamId!.Value)
            .ToArray();

        if (winners.Length == 1)
        {
            CompleteTournament(tournament, winners);
            return Task.CompletedTask;
        }

        var winnerTeams = winners
            .Select(id => tournament.Teams.Single(team => team.Id == id))
            .ToArray();
        CreateRound(tournament, round.Number + 1, BracketType.Main, winnerTeams);
        return Task.CompletedTask;
    }
}
