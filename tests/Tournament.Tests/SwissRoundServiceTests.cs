using Tournament.Application.Brackets;
using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Dto;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Tournament.Tests;

public sealed class SwissRoundServiceTests
{
    [Fact]
    public async Task CreateNextRound_AfterAllMatchesCompleted_AdvancesCurrentRoundNumber()
    {
        var fixture = await CreateSwissFixtureAsync();
        CompleteAllMatchesInLatestRound(fixture);
        Assert.Equal(1, fixture.Tournament.CurrentRoundNumber);

        var result = await fixture.Service.CreateNextRoundAsync(
            fixture.Tournament.Id,
            new CurrentTournamentUser(fixture.Tournament.OrganizerId, "Organizer", "Active"));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, fixture.Tournament.Rounds.Count);
        Assert.Equal(2, fixture.Tournament.CurrentRoundNumber);
    }

    [Fact]
    public async Task CreateNextRound_CalledTwice_SecondCallReturnsCurrentRoundNotCompleted()
    {
        var fixture = await CreateSwissFixtureAsync();
        CompleteAllMatchesInLatestRound(fixture);
        var organizer = new CurrentTournamentUser(fixture.Tournament.OrganizerId, "Organizer", "Active");

        var first = await fixture.Service.CreateNextRoundAsync(fixture.Tournament.Id, organizer);
        var second = await fixture.Service.CreateNextRoundAsync(fixture.Tournament.Id, organizer);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal(TournamentErrors.CurrentRoundNotCompleted, second.Error);
        // The misleading RegistrationClosed error must no longer be reused for this case.
        Assert.NotEqual(TournamentErrors.TournamentRegistrationClosed, second.Error);
    }

    [Fact]
    public async Task CreateNextRound_OnNonSwissTournament_Fails()
    {
        var fixture = await CreateSeFixtureAsync();

        var result = await fixture.Service.CreateNextRoundAsync(
            fixture.Tournament.Id,
            new CurrentTournamentUser(fixture.Tournament.OrganizerId, "Organizer", "Active"));

        Assert.True(result.IsFailure);
    }

    private static async Task<Fixture> CreateSwissFixtureAsync()
    {
        var tournament = Domain.Tournaments.Tournament.Create(
            "Swiss Cup",
            "SWISS CUP",
            null,
            "CS2",
            TournamentFormat.Swiss,
            swissRounds: 3,
            teamSize: 1,
            maxPlayers: 4,
            organizerId: Guid.NewGuid(),
            createdAtUtc: DateTime.UtcNow);

        for (var index = 1; index <= 4; index++)
        {
            tournament.AddTeams([Team.Create(
                tournament.Id,
                $"Team{index}",
                Guid.NewGuid(),
                index,
                1000 + index,
                [TeamMember.Create(Guid.NewGuid(), $"Player{index}", 1000 + index)])]);
        }

        tournament.Start(DateTime.UtcNow);

        var outbox = new InMemoryOutboxWriter();
        var swissGenerator = new SwissBracketGenerator(outbox);
        await swissGenerator.GenerateInitialAsync(tournament, tournament.Teams.ToArray(), CancellationToken.None);

        var factory = new BracketGeneratorFactory(new IBracketGenerator[]
        {
            new SingleEliminationBracketGenerator(outbox),
            new DoubleEliminationBracketGenerator(outbox),
            swissGenerator
        });
        var service = new SwissRoundService(new InMemoryTournamentRepository(tournament), factory);
        return new Fixture(tournament, swissGenerator, service);
    }

    private static async Task<Fixture> CreateSeFixtureAsync()
    {
        var tournament = Domain.Tournaments.Tournament.Create(
            "SE Cup",
            "SE CUP",
            null,
            "CS2",
            TournamentFormat.SingleElimination,
            swissRounds: null,
            teamSize: 1,
            maxPlayers: 2,
            organizerId: Guid.NewGuid(),
            createdAtUtc: DateTime.UtcNow);

        tournament.AddTeams([
            Team.Create(tournament.Id, "A", Guid.NewGuid(), 1, 1100, [TeamMember.Create(Guid.NewGuid(), "A", 1100)]),
            Team.Create(tournament.Id, "B", Guid.NewGuid(), 2, 1000, [TeamMember.Create(Guid.NewGuid(), "B", 1000)])
        ]);

        tournament.Start(DateTime.UtcNow);

        var outbox = new InMemoryOutboxWriter();
        var seGenerator = new SingleEliminationBracketGenerator(outbox);
        await seGenerator.GenerateInitialAsync(tournament, tournament.Teams.ToArray(), CancellationToken.None);

        var factory = new BracketGeneratorFactory(new IBracketGenerator[]
        {
            seGenerator,
            new DoubleEliminationBracketGenerator(outbox),
            new SwissBracketGenerator(outbox)
        });
        var service = new SwissRoundService(new InMemoryTournamentRepository(tournament), factory);
        return new Fixture(tournament, seGenerator, service);
    }

    private static void CompleteAllMatchesInLatestRound(Fixture fixture)
    {
        var round = fixture.Tournament.Rounds
            .OrderByDescending(r => r.Number)
            .First();

        foreach (var match in round.Matches.Where(m => m.Status != MatchStatus.Completed))
        {
            match.Complete(match.TeamAId!.Value, 1, 0, 1, 0, false, DateTime.UtcNow);
            fixture.Generator.HandleMatchCompletedAsync(fixture.Tournament, match, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
    }

    private sealed record Fixture(
        Domain.Tournaments.Tournament Tournament,
        IBracketGenerator Generator,
        SwissRoundService Service);

    private sealed class InMemoryOutboxWriter : IOutboxWriter
    {
        public List<IntegrationEvent> Events { get; } = [];
        public void Add(IntegrationEvent integrationEvent) => Events.Add(integrationEvent);
    }

    private sealed class InMemoryTournamentRepository(Domain.Tournaments.Tournament tournament) : ITournamentRepository
    {
        public Task<bool> TitleExistsAsync(string normalizedTitle, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Discipline?> GetActiveDisciplineAsync(string disciplineCode, CancellationToken cancellationToken = default) => Task.FromResult<Discipline?>(null);
        public Task<Domain.Tournaments.Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == tournament.Id ? tournament : null);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByStatusAsync(TournamentStatus status, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<bool> BlockedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ITournamentTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void Add(Domain.Tournaments.Tournament value) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
