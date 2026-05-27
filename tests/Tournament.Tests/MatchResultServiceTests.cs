using Tournament.Application.Brackets;
using Tournament.Application.Matches;
using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Dto;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Tournament.Tests;

public sealed class MatchResultServiceTests
{
    [Fact]
    public async Task OrganizerOwner_CanCompleteMatch_AndPublishesMatchCompletedOnce()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(fixture.Match.TeamAId!.Value, 2, 0, false),
            new CurrentTournamentUser(fixture.Tournament.OrganizerId, "Organizer", "Active"));

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Completed, fixture.Match.Status);
        Assert.Single(fixture.Outbox.Events.OfType<MatchCompletedEvent>());
    }

    [Fact]
    public async Task OtherOrganizer_GetsAccessDenied()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(fixture.Match.TeamAId!.Value, 2, 0, false),
            new CurrentTournamentUser(Guid.NewGuid(), "Organizer", "Active"));

        Assert.True(result.IsFailure);
        Assert.Equal(Tournament.Application.Tournaments.TournamentErrors.AccessDenied, result.Error);
    }

    [Fact]
    public async Task Player_GetsAccessDenied()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(fixture.Match.TeamAId!.Value, 2, 0, false),
            new CurrentTournamentUser(Guid.NewGuid(), "Player", "Active"));

        Assert.True(result.IsFailure);
        Assert.Equal(Tournament.Application.Tournaments.TournamentErrors.AccessDenied, result.Error);
    }

    [Fact]
    public async Task Admin_CanCompleteAnyMatch()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(fixture.Match.TeamAId!.Value, 2, 0, false),
            new CurrentTournamentUser(Guid.NewGuid(), "Admin", "Active"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task InvalidWinner_ReturnsFailure()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(Guid.NewGuid(), 2, 0, false),
            new CurrentTournamentUser(Guid.NewGuid(), "Admin", "Active"));

        Assert.True(result.IsFailure);
        Assert.Equal(Tournament.Application.Tournaments.TournamentErrors.InvalidWinnerTeam, result.Error);
    }

    [Fact]
    public async Task WinnerScoreMustBeGreaterThanLoserScore()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(fixture.Match.TeamAId!.Value, 1, 1, false),
            new CurrentTournamentUser(Guid.NewGuid(), "Admin", "Active"));

        Assert.True(result.IsFailure);
        Assert.Equal(Tournament.Application.Tournaments.TournamentErrors.InvalidMatchScore, result.Error);
    }

    [Fact]
    public async Task RegularMatchWithoutScore_ReturnsFailure()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(fixture.Match.TeamAId!.Value, null, null, false),
            new CurrentTournamentUser(Guid.NewGuid(), "Admin", "Active"));

        Assert.True(result.IsFailure);
        Assert.Equal(Tournament.Application.Tournaments.TournamentErrors.MatchScoreRequired, result.Error);
        Assert.Empty(fixture.Outbox.Events.OfType<MatchCompletedEvent>());
    }

    [Fact]
    public async Task TechnicalDefeatWithoutScore_Succeeds_AndStoresNullScore()
    {
        var fixture = CreateFixture();

        var result = await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(fixture.Match.TeamAId!.Value, null, null, true),
            new CurrentTournamentUser(Guid.NewGuid(), "Admin", "Active"));

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Completed, fixture.Match.Status);
        Assert.Null(fixture.Match.WinnerScore);
        Assert.Null(fixture.Match.LoserScore);
        Assert.True(fixture.Match.IsTechnicalDefeat);
        Assert.Single(fixture.Outbox.Events.OfType<MatchCompletedEvent>());
    }

    [Fact]
    public async Task CompletingAlreadyCompletedMatch_ReturnsConflictAndDoesNotPublishTwice()
    {
        var fixture = CreateFixture();
        var admin = new CurrentTournamentUser(Guid.NewGuid(), "Admin", "Active");

        await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(fixture.Match.TeamAId!.Value, 2, 0, false),
            admin);
        var result = await fixture.Service.CompleteMatchAsync(
            fixture.Tournament.Id,
            fixture.Match.Id,
            new CompleteMatchRequest(fixture.Match.TeamAId!.Value, 2, 0, false),
            admin);

        Assert.True(result.IsFailure);
        Assert.Equal(Tournament.Application.Tournaments.TournamentErrors.MatchAlreadyCompleted, result.Error);
        Assert.Single(fixture.Outbox.Events.OfType<MatchCompletedEvent>());
    }

    private static Fixture CreateFixture()
    {
        var tournament = Domain.Tournaments.Tournament.Create(
            "Match Cup",
            "MATCH CUP",
            null,
            "CS2",
            TournamentFormat.SingleElimination,
            null,
            1,
            2,
            Guid.NewGuid(),
            DateTime.UtcNow);

        var teamA = Team.Create(tournament.Id, "A", Guid.NewGuid(), 1, 1100, [TeamMember.Create(Guid.NewGuid(), "A", 1100)]);
        var teamB = Team.Create(tournament.Id, "B", Guid.NewGuid(), 2, 1000, [TeamMember.Create(Guid.NewGuid(), "B", 1000)]);
        tournament.AddTeams([teamA, teamB]);
        var round = Round.Create(tournament.Id, 1, BracketType.Main, DateTime.UtcNow);
        round.Start();
        var match = Match.Create(tournament.Id, 1, teamA.Id, teamB.Id, DateTime.UtcNow);
        round.AddMatch(match);
        tournament.AddRound(round);
        tournament.Start(DateTime.UtcNow);

        var repository = new InMemoryTournamentRepository(tournament);
        var outbox = new InMemoryOutboxWriter();
        IBracketGenerator[] generators =
        [
            new SingleEliminationBracketGenerator(outbox),
            new DoubleEliminationBracketGenerator(outbox),
            new SwissBracketGenerator(outbox)
        ];
        var service = new MatchResultService(repository, new BracketGeneratorFactory(generators), outbox);
        return new Fixture(tournament, match, service, outbox);
    }

    private sealed record Fixture(
        Domain.Tournaments.Tournament Tournament,
        Match Match,
        MatchResultService Service,
        InMemoryOutboxWriter Outbox);

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
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([ToSummary(tournament)]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByStatusAsync(TournamentStatus status, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<bool> DeletedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ITournamentTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
}
