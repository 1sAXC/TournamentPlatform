using Tournament.Application.Tournaments.Abstractions;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Brackets;

public sealed class SingleEliminationBracketGenerator(IOutboxWriter outboxWriter)
    : BracketGeneratorBase(outboxWriter)
{
    protected override BracketType BracketType => BracketType.Main;

    public override Task HandleMatchCompletedAsync(
        Domain.Tournaments.Tournament tournament,
        Match completedMatch,
        CancellationToken ct)
    {
        var round = FindRound(tournament, completedMatch);
        if (round is null || round.Matches.Any(match => match.Status != MatchStatus.Completed))
        {
            return Task.CompletedTask;
        }

        round.Complete(DateTime.UtcNow);

        if (round.BracketType == BracketType.ThirdPlace)
        {
            TryCompleteAfterFinalAndThirdPlace(tournament);
            return Task.CompletedTask;
        }

        // Main bracket round just finished.
        var winners = round.Matches
            .Where(match => match.WinnerTeamId.HasValue)
            .Select(match => match.WinnerTeamId!.Value)
            .ToArray();
        var losers = round.Matches
            .Where(match => match.LoserTeamId.HasValue)
            .Select(match => match.LoserTeamId!.Value)
            .ToArray();

        if (winners.Length == 1)
        {
            // The Final has finished. Wait for the 3rd-place match if it exists.
            TryCompleteAfterFinalAndThirdPlace(tournament);
            return Task.CompletedTask;
        }

        var winnerTeams = winners
            .Select(id => tournament.Teams.Single(team => team.Id == id))
            .OrderByDescending(team => team.AverageElo)
            .ThenBy(team => team.Id)
            .ToArray();
        CreateRound(tournament, round.Number + 1, BracketType.Main, winnerTeams);

        // The semifinal produces exactly two losers, who play for 3rd place
        // alongside the final. Earlier rounds (>2 winners) just advance the
        // winners forward.
        if (winners.Length == 2 && losers.Length == 2)
        {
            var loserTeams = losers
                .Select(id => tournament.Teams.Single(team => team.Id == id))
                .OrderByDescending(team => team.AverageElo)
                .ThenBy(team => team.Id)
                .ToArray();
            CreateRound(tournament, round.Number + 1, BracketType.ThirdPlace, loserTeams);
        }

        return Task.CompletedTask;
    }

    private void TryCompleteAfterFinalAndThirdPlace(Domain.Tournaments.Tournament tournament)
    {
        var final = tournament.Rounds
            .Where(r => r.BracketType == BracketType.Main)
            .OrderByDescending(r => r.Number)
            .FirstOrDefault(r => r.Matches.Count == 1);
        if (final is null || final.Status != RoundStatus.Completed)
        {
            return;
        }

        var thirdPlace = tournament.Rounds.SingleOrDefault(r =>
            r.BracketType == BracketType.ThirdPlace && r.Number == final.Number);
        if (thirdPlace is not null && thirdPlace.Status != RoundStatus.Completed)
        {
            return;
        }

        CompleteTournament(tournament);
    }
}
