using Tournament.Domain.Tournaments;
using Tournament.Application.Tournaments.Abstractions;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Brackets;

public sealed class SwissBracketGenerator(IOutboxWriter outboxWriter)
    : BracketGeneratorBase(outboxWriter)
{
    protected override BracketType BracketType => BracketType.Swiss;

    public override Task GenerateInitialAsync(
        Domain.Tournaments.Tournament tournament,
        IReadOnlyList<Team> teams,
        CancellationToken ct)
    {
        if (tournament.Rounds.Count > 0)
        {
            return Task.CompletedTask;
        }

        AddInitialStandings(tournament, teams);
        CreateAdjacentRound(tournament, 1, SeedForInitialRound(teams));
        return Task.CompletedTask;
    }

    public override Task HandleMatchCompletedAsync(
        Domain.Tournaments.Tournament tournament,
        Match completedMatch,
        CancellationToken ct)
    {
        var winner = completedMatch.WinnerTeamId;
        var loser = completedMatch.LoserTeamId;
        if (winner is not null)
        {
            tournament.SwissStandings.Single(standing => standing.TeamId == winner).AddWin();
        }

        if (loser is not null)
        {
            tournament.SwissStandings.Single(standing => standing.TeamId == loser).AddLoss();
        }

        var round = FindRound(tournament, completedMatch);
        if (round is null || round.Matches.Any(match => match.Status != MatchStatus.Completed))
        {
            return Task.CompletedTask;
        }

        round.Complete(DateTime.UtcNow);
        if (round.Number == tournament.SwissRounds)
        {
            var standings = tournament.SwissStandings
                .OrderByDescending(standing => standing.Points)
                .ThenByDescending(standing => standing.Wins)
                .Select(standing => standing.TeamId)
                .ToArray();
            CompleteTournament(tournament, standings);
        }

        return Task.CompletedTask;
    }

    public void CreateNextRound(Domain.Tournaments.Tournament tournament)
    {
        var lastRound = tournament.Rounds
            .Where(round => round.BracketType == BracketType.Swiss)
            .OrderByDescending(round => round.Number)
            .First();

        var orderedTeams = tournament.SwissStandings
            .OrderByDescending(standing => standing.Points)
            .ThenByDescending(standing => standing.Wins)
            .Select(standing => tournament.Teams.Single(team => team.Id == standing.TeamId))
            .OrderByDescending(team => tournament.SwissStandings.Single(s => s.TeamId == team.Id).Points)
            .ThenByDescending(team => tournament.SwissStandings.Single(s => s.TeamId == team.Id).Wins)
            .ThenByDescending(team => team.AverageElo)
            .ThenBy(team => team.Id)
            .ToArray();

        CreateAdjacentRound(tournament, lastRound.Number + 1, orderedTeams);
    }

    protected override void AddInitialStandings(Domain.Tournaments.Tournament tournament, IReadOnlyList<Team> teams)
    {
        if (tournament.SwissStandings.Count > 0)
        {
            return;
        }

        foreach (var team in teams)
        {
            tournament.AddSwissStanding(SwissStanding.Create(tournament.Id, team.Id));
        }
    }

    private void CreateAdjacentRound(
        Domain.Tournaments.Tournament tournament,
        int roundNumber,
        IReadOnlyList<Team> orderedTeams)
    {
        var now = DateTime.UtcNow;
        var round = Round.Create(tournament.Id, roundNumber, BracketType.Swiss, now);
        round.Start();

        var matchNumber = 1;
        for (var i = 0; i < orderedTeams.Count; i += 2)
        {
            var teamA = orderedTeams[i];
            var teamB = i + 1 < orderedTeams.Count ? orderedTeams[i + 1] : null;
            var match = Match.Create(tournament.Id, matchNumber++, teamA.Id, teamB?.Id, now);
            if (teamB is null)
            {
                match.CompleteBye(teamA.Id, now);
            }

            round.AddMatch(match);
        }

        tournament.AddRound(round);
        tournament.AdvanceToRound(roundNumber);
    }
}
