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
        Assert.Equal(2, tournament.CurrentRoundNumber);
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
        Assert.Equal(2, tournament.CurrentRoundNumber);
    }

    [Fact]
    public async Task SingleElimination_EmitsRoundCreatedEventForInitialAndSubsequentRound()
    {
        var outbox = new InMemoryOutboxWriter();
        var generator = new SingleEliminationBracketGenerator(outbox);
        var tournament = TournamentWithTeams(TournamentFormat.SingleElimination, 4);

        await generator.GenerateInitialAsync(tournament, tournament.Teams.ToArray(), CancellationToken.None);

        var firstEvent = Assert.Single(outbox.Events.OfType<RoundCreatedEvent>());
        Assert.Equal(1, firstEvent.RoundNumber);
        Assert.Equal(tournament.Id, firstEvent.TournamentId);
        Assert.Equal(tournament.Title, firstEvent.TournamentTitle);
        Assert.Equal(2, firstEvent.Matches.Count);
        Assert.Equal(4, firstEvent.Teams.Count);
        Assert.All(firstEvent.Teams, team => Assert.NotEmpty(team.Members));

        foreach (var match in tournament.Rounds.Single().Matches)
        {
            match.Complete(match.TeamAId!.Value, 1, 0, false, DateTime.UtcNow);
            await generator.HandleMatchCompletedAsync(tournament, match, CancellationToken.None);
        }

        var secondEvent = outbox.Events.OfType<RoundCreatedEvent>().ElementAt(1);
        Assert.Equal(2, secondEvent.RoundNumber);
        Assert.Single(secondEvent.Matches);
        Assert.Equal(2, secondEvent.Teams.Count);
    }

    [Fact]
    public async Task Swiss_EmitsRoundCreatedEventOnInitialAndManualNextRound()
    {
        var outbox = new InMemoryOutboxWriter();
        var generator = new SwissBracketGenerator(outbox);
        var tournament = TournamentWithTeams(TournamentFormat.Swiss, 4);

        await generator.GenerateInitialAsync(tournament, tournament.Teams.ToArray(), CancellationToken.None);
        Assert.Single(outbox.Events.OfType<RoundCreatedEvent>());

        foreach (var match in tournament.Rounds.Single().Matches)
        {
            match.Complete(match.TeamAId!.Value, 1, 0, false, DateTime.UtcNow);
            await generator.HandleMatchCompletedAsync(tournament, match, CancellationToken.None);
        }

        generator.CreateNextRound(tournament);

        var events = outbox.Events.OfType<RoundCreatedEvent>().ToArray();
        Assert.Equal(2, events.Length);
        Assert.Equal(1, events[0].RoundNumber);
        Assert.Equal(2, events[1].RoundNumber);
        Assert.NotEqual(events[0].EventId, events[1].EventId);
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
