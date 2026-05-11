using Tournament.Domain.Tournaments;
using Tournament.Application.Tournaments.Abstractions;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Brackets;

public sealed class DoubleEliminationBracketGenerator(IOutboxWriter outboxWriter)
    : BracketGeneratorBase(outboxWriter)
{
    protected override BracketType BracketType => BracketType.Upper;

    public override Task HandleMatchCompletedAsync(
        Domain.Tournaments.Tournament tournament,
        Match completedMatch,
        CancellationToken ct)
    {
        if (completedMatch.LoserTeamId is not null)
        {
            tournament.DoubleEliminationStandings
                .Single(standing => standing.TeamId == completedMatch.LoserTeamId)
                .AddLoss();
        }

        var round = FindRound(tournament, completedMatch);
        if (round is null || round.Matches.Any(match => match.Status != MatchStatus.Completed))
        {
            return Task.CompletedTask;
        }

        round.Complete(DateTime.UtcNow);
        var active = tournament.DoubleEliminationStandings
            .Where(standing => !standing.IsEliminated)
            .ToArray();

        if (active.Length == 1)
        {
            CompleteTournament(tournament, [active.Single().TeamId]);
            return Task.CompletedTask;
        }

        if (round.BracketType == BracketType.GrandFinal && active.Length == 2)
        {
            var resetTeams = active
                .Select(standing => tournament.Teams.Single(team => team.Id == standing.TeamId))
                .OrderByDescending(team => team.AverageElo)
                .ThenBy(team => team.Id)
                .ToArray();
            CreateRound(tournament, round.Number + 1, BracketType.GrandFinal, resetTeams);
            return Task.CompletedTask;
        }

        var undefeated = active
            .Where(standing => standing.Losses == 0)
            .Select(standing => tournament.Teams.Single(team => team.Id == standing.TeamId))
            .OrderByDescending(team => team.AverageElo)
            .ThenBy(team => team.Id)
            .ToArray();
        var oneLoss = active
            .Where(standing => standing.Losses == 1)
            .Select(standing => tournament.Teams.Single(team => team.Id == standing.TeamId))
            .OrderByDescending(team => team.AverageElo)
            .ThenBy(team => team.Id)
            .ToArray();

        if ((undefeated.Length == 1 && oneLoss.Length == 1)
            || (undefeated.Length == 0 && oneLoss.Length == 2))
        {
            var finalTeams = undefeated.Concat(oneLoss)
                .OrderBy(TeamLosses)
                .ThenByDescending(team => team.AverageElo)
                .ThenBy(team => team.Id)
                .ToArray();
            CreateRound(tournament, round.Number + 1, BracketType.GrandFinal, finalTeams);
            return Task.CompletedTask;
        }

        if (undefeated.Length > 1)
        {
            CreateRound(tournament, round.Number + 1, BracketType.Upper, undefeated);
        }

        if (oneLoss.Length > 1)
        {
            CreateRound(tournament, round.Number + 1, BracketType.Lower, oneLoss);
        }

        return Task.CompletedTask;

        int TeamLosses(Team team)
        {
            return tournament.DoubleEliminationStandings.Single(standing => standing.TeamId == team.Id).Losses;
        }
    }

    protected override void AddInitialStandings(Domain.Tournaments.Tournament tournament, IReadOnlyList<Team> teams)
    {
        if (tournament.DoubleEliminationStandings.Count > 0)
        {
            return;
        }

        foreach (var team in teams)
        {
            tournament.AddDoubleEliminationStanding(DoubleEliminationStanding.Create(tournament.Id, team.Id));
        }
    }
}
