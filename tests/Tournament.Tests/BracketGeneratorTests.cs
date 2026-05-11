using Tournament.Application.Brackets;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Tournament.Tests;

public sealed class BracketGeneratorTests
{
    [Fact]
    public async Task SingleElimination_FourTeams_ProgressesToFinalAndCompletes()
    {
        var outbox = new InMemoryOutboxWriter();
        var generator = new SingleEliminationBracketGenerator(outbox);
        var tournament = TournamentWithTeams(TournamentFormat.SingleElimination, 4);

        await generator.GenerateInitialAsync(tournament, tournament.Teams.ToArray(), CancellationToken.None);
        var semiFinal = tournament.Rounds.Single();
        Assert.Equal(2, semiFinal.Matches.Count);

        foreach (var match in semiFinal.Matches)
        {
            match.Complete(match.TeamAId!.Value, 1, 0, false, DateTime.UtcNow);
            await generator.HandleMatchCompletedAsync(tournament, match, CancellationToken.None);
        }

        Assert.Equal(2, tournament.Rounds.Count);
        var final = tournament.Rounds.Single(round => round.Number == 2);
        var finalMatch = final.Matches.Single();
        finalMatch.Complete(finalMatch.TeamAId!.Value, 1, 0, false, DateTime.UtcNow);
        await generator.HandleMatchCompletedAsync(tournament, finalMatch, CancellationToken.None);

        Assert.Equal(TournamentStatus.Completed, tournament.Status);
        Assert.Single(outbox.Events.OfType<TournamentCompletedEvent>());
    }

    [Fact]
    public async Task SingleElimination_ThreeTeams_CreatesByeAndFinal()
    {
        var generator = new SingleEliminationBracketGenerator(new InMemoryOutboxWriter());
        var tournament = TournamentWithTeams(TournamentFormat.SingleElimination, 3);

        await generator.GenerateInitialAsync(tournament, tournament.Teams.ToArray(), CancellationToken.None);
        var round = tournament.Rounds.Single();

        Assert.Contains(round.Matches, match => match.TeamBId is null && match.Status == MatchStatus.Completed);
        var playable = round.Matches.Single(match => match.TeamBId is not null);
        playable.Complete(playable.TeamAId!.Value, 1, 0, false, DateTime.UtcNow);
        await generator.HandleMatchCompletedAsync(tournament, playable, CancellationToken.None);

        Assert.Equal(2, tournament.Rounds.Count);
        Assert.Single(tournament.Rounds.Single(r => r.Number == 2).Matches);
    }

    [Fact]
    public async Task SingleElimination_DoesNotCreateNextRoundUntilAllMatchesCompleted()
    {
        var generator = new SingleEliminationBracketGenerator(new InMemoryOutboxWriter());
        var tournament = TournamentWithTeams(TournamentFormat.SingleElimination, 4);
        await generator.GenerateInitialAsync(tournament, tournament.Teams.ToArray(), CancellationToken.None);
        var match = tournament.Rounds.Single().Matches.First();

        match.Complete(match.TeamAId!.Value, 1, 0, false, DateTime.UtcNow);
        await generator.HandleMatchCompletedAsync(tournament, match, CancellationToken.None);

        Assert.Single(tournament.Rounds);
    }

    [Fact]
    public async Task Swiss_UpdatesStandingsAndRequiresManualNextRound()
    {
        var generator = new SwissBracketGenerator(new InMemoryOutboxWriter());
        var tournament = TournamentWithTeams(TournamentFormat.Swiss, 4);
        await generator.GenerateInitialAsync(tournament, tournament.Teams.ToArray(), CancellationToken.None);

        var round = tournament.Rounds.Single();
        foreach (var match in round.Matches)
        {
            match.Complete(match.TeamAId!.Value, 1, 0, false, DateTime.UtcNow);
            await generator.HandleMatchCompletedAsync(tournament, match, CancellationToken.None);
        }

        Assert.Equal(RoundStatus.Completed, round.Status);
        Assert.Single(tournament.Rounds);
        Assert.Contains(tournament.SwissStandings, standing => standing.Points == 3 && standing.Wins == 1);

        generator.CreateNextRound(tournament);
        Assert.Equal(2, tournament.Rounds.Count);
    }

    [Fact]
    public async Task DoubleElimination_TeamEliminatedAfterSecondLoss()
    {
        var generator = new DoubleEliminationBracketGenerator(new InMemoryOutboxWriter());
        var tournament = TournamentWithTeams(TournamentFormat.DoubleElimination, 2);
        await generator.GenerateInitialAsync(tournament, tournament.Teams.ToArray(), CancellationToken.None);
        var loser = tournament.Rounds.Single().Matches.Single().TeamBId!.Value;

        tournament.DoubleEliminationStandings.Single(s => s.TeamId == loser).AddLoss();
        var match = tournament.Rounds.Single().Matches.Single();
        match.Complete(match.TeamAId!.Value, 1, 0, false, DateTime.UtcNow);
        await generator.HandleMatchCompletedAsync(tournament, match, CancellationToken.None);

        Assert.True(tournament.DoubleEliminationStandings.Single(s => s.TeamId == loser).IsEliminated);
        Assert.Equal(TournamentStatus.Completed, tournament.Status);
    }

    private static Domain.Tournaments.Tournament TournamentWithTeams(TournamentFormat format, int teamCount)
    {
        var tournament = Domain.Tournaments.Tournament.Create(
            $"Bracket {format}",
            $"BRACKET {format}",
            null,
            "CS2",
            format,
            format == TournamentFormat.Swiss ? 3 : null,
            1,
            teamCount,
            Guid.NewGuid(),
            DateTime.UtcNow);

        tournament.AddTeams(Enumerable.Range(1, teamCount).Select(index =>
            Team.Create(
                tournament.Id,
                $"Team{index}",
                Guid.NewGuid(),
                index,
                1000 + index,
                [TeamMember.Create(Guid.NewGuid(), $"Player{index}", 1000 + index)])));

        return tournament;
    }

    private sealed class InMemoryOutboxWriter : IOutboxWriter
    {
        public List<IntegrationEvent> Events { get; } = [];
        public void Add(IntegrationEvent integrationEvent) => Events.Add(integrationEvent);
    }
}
