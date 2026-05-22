using Tournament.Application.Brackets;
using Tournament.Application.TeamBalancer;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Dto;
using Tournament.Application.Tournaments.Services;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Tournament.Tests;

public sealed class TournamentLifecycleServiceTests
{
    [Theory]
    [InlineData(TournamentFormat.SingleElimination)]
    [InlineData(TournamentFormat.DoubleElimination)]
    [InlineData(TournamentFormat.Swiss)]
    public async Task TryStartTournamentAsync_CreatesTeamsMatchesAndPublishesEvent(TournamentFormat format)
    {
        var tournament = CreateReadyTournament(format, teamSize: 2, playersCount: 4);
        var tournaments = new InMemoryTournamentRepository(tournament);
        var ratings = new InMemoryProjectionRepository();
        var outbox = new InMemoryOutboxWriter();
        SeedRatings(tournament, ratings);
        var service = CreateService(tournaments, ratings, outbox);

        await service.TryStartTournamentAsync(tournament);

        Assert.Equal(TournamentStatus.InProgress, tournament.Status);
        Assert.NotNull(tournament.StartedAtUtc);
        Assert.Equal(2, tournament.Teams.Count);
        Assert.Single(tournament.Rounds);
        Assert.Single(tournament.Rounds.Single().Matches);
        Assert.Single(outbox.Events.OfType<TournamentStartedEvent>());
    }

    [Fact]
    public async Task TryStartTournamentAsync_CreatesMissingRatingProjectionWithDefaultElo()
    {
        var tournament = CreateReadyTournament(TournamentFormat.SingleElimination, teamSize: 1, playersCount: 2);
        var tournaments = new InMemoryTournamentRepository(tournament);
        var ratings = new InMemoryProjectionRepository();
        var service = CreateService(tournaments, ratings, new InMemoryOutboxWriter());

        await service.TryStartTournamentAsync(tournament);

        Assert.Equal(2, ratings.Projections.Count);
        Assert.All(ratings.Projections, projection => Assert.Equal(1000, projection.Elo));
    }

    [Fact]
    public async Task TryStartTournamentAsync_IsIdempotent()
    {
        var tournament = CreateReadyTournament(TournamentFormat.SingleElimination, teamSize: 2, playersCount: 4);
        var tournaments = new InMemoryTournamentRepository(tournament);
        var ratings = new InMemoryProjectionRepository();
        var outbox = new InMemoryOutboxWriter();
        SeedRatings(tournament, ratings);
        var service = CreateService(tournaments, ratings, outbox);

        await service.TryStartTournamentAsync(tournament);
        await service.TryStartTournamentAsync(tournament);

        Assert.Equal(2, tournament.Teams.Count);
        Assert.Single(tournament.Rounds);
        Assert.Single(tournament.Rounds.Single().Matches);
        Assert.Single(outbox.Events.OfType<TournamentStartedEvent>());
    }

    private static TournamentLifecycleService CreateService(
        InMemoryTournamentRepository tournaments,
        InMemoryProjectionRepository ratings,
        InMemoryOutboxWriter outbox)
    {
        IBracketGenerator[] generators =
        [
            new SingleEliminationBracketGenerator(outbox),
            new DoubleEliminationBracketGenerator(outbox),
            new SwissBracketGenerator(outbox)
        ];

        return new TournamentLifecycleService(
            ratings,
            new GreedyTeamBalancer(new DeterministicRandomProvider()),
            new BracketGeneratorFactory(generators),
            outbox);
    }

    private static Domain.Tournaments.Tournament CreateReadyTournament(
        TournamentFormat format,
        int teamSize,
        int playersCount)
    {
        var tournament = Domain.Tournaments.Tournament.Create(
            $"Lifecycle {format}",
            $"LIFECYCLE {format}".ToUpperInvariant(),
            null,
            "CS2",
            format,
            format == TournamentFormat.Swiss ? 3 : null,
            teamSize,
            playersCount,
            Guid.NewGuid(),
            DateTime.UtcNow);

        for (var i = 0; i < playersCount; i++)
        {
            tournament.RegisterParticipant(Guid.NewGuid(), $"Player{i + 1}", DateTime.UtcNow.AddSeconds(i));
        }

        return tournament;
    }

    private static void SeedRatings(
        Domain.Tournaments.Tournament tournament,
        InMemoryProjectionRepository ratings)
    {
        var elo = 1000;
        foreach (var participant in tournament.Participants)
        {
            ratings.Add(PlayerRatingProjection.Create(
                participant.PlayerId,
                tournament.DisciplineCode,
                elo += 25,
                DateTime.UtcNow));
        }
    }

    private sealed class InMemoryTournamentRepository(Domain.Tournaments.Tournament tournament) : ITournamentRepository
    {
        public Task<bool> TitleExistsAsync(string normalizedTitle, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Discipline?> GetActiveDisciplineAsync(string disciplineCode, CancellationToken cancellationToken = default) => Task.FromResult<Discipline?>(null);
        public Task<Domain.Tournaments.Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == tournament.Id ? tournament : null);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([ToSummary(tournament)]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByStatusAsync(TournamentStatus status, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<bool> DeletedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ITournamentTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult<ITournamentTransaction>(new NoopTransaction());
        public void Add(Domain.Tournaments.Tournament value) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private static TournamentSummaryDto ToSummary(Domain.Tournaments.Tournament value)
        {
            return new TournamentSummaryDto(
                value.Id,
                value.Title,
                value.Description,
                value.DisciplineCode,
                value.Format,
                value.SwissRounds,
                value.TeamSize,
                value.MaxPlayers,
                value.OrganizerId,
                value.Status,
                value.CurrentRoundNumber,
                value.ActiveParticipantsCount,
                value.CreatedAtUtc,
                value.StartedAtUtc,
                value.CompletedAtUtc,
                value.CancelledAtUtc);
        }
    }

    private sealed class InMemoryProjectionRepository : IPlayerRatingProjectionRepository
    {
        public List<PlayerRatingProjection> Projections { get; } = [];
        public Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PlayerRatingProjection>>(Projections.Where(projection => projection.PlayerId == playerId).ToArray());
        public Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdsAsync(IReadOnlyCollection<Guid> playerIds, string disciplineCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PlayerRatingProjection>>(Projections.Where(projection => playerIds.Contains(projection.PlayerId) && projection.DisciplineCode == disciplineCode).ToArray());
        public Task<PlayerRatingProjection?> GetAsync(Guid playerId, string disciplineCode, CancellationToken cancellationToken = default) => Task.FromResult(Projections.FirstOrDefault(projection => projection.PlayerId == playerId && projection.DisciplineCode == disciplineCode));
        public Task<bool> DeletedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddDeletedUserAsync(Guid userId, DateTime deletedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Add(PlayerRatingProjection projection) => Projections.Add(projection);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryOutboxWriter : IOutboxWriter
    {
        public List<IntegrationEvent> Events { get; } = [];
        public void Add(IntegrationEvent integrationEvent) => Events.Add(integrationEvent);
    }

    private sealed class NoopTransaction : ITournamentTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class DeterministicRandomProvider : IRandomProvider
    {
        public int Next(int maxExclusive) => 0;
    }
}
