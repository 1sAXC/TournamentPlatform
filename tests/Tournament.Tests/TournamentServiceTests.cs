using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Dto;
using Tournament.Application.Tournaments.Services;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Common;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Contracts.Enums;

namespace Tournament.Tests;

public sealed class TournamentServiceTests
{
    private static readonly CurrentTournamentUser ActiveOrganizer = new(Guid.NewGuid(), "Organizer", "Active");

    [Fact]
    public async Task ActiveOrganizer_CanCreateTournament()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);

        var result = await service.CreateAsync(ValidRequest("Stage8 Cup"), ActiveOrganizer);

        Assert.True(result.IsSuccess);
        Assert.Equal("Open", result.Value.Status);
        Assert.Single(repository.Tournaments);
    }

    [Fact]
    public async Task PendingOrganizer_GetsAccessDenied()
    {
        var service = CreateService(new InMemoryTournamentRepository());

        var result = await service.CreateAsync(
            ValidRequest("Stage8 Cup"),
            new CurrentTournamentUser(Guid.NewGuid(), "Organizer", "PendingApproval"));

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.AccessDenied, result.Error);
    }

    [Fact]
    public async Task Player_GetsAccessDenied()
    {
        var service = CreateService(new InMemoryTournamentRepository());

        var result = await service.CreateAsync(
            ValidRequest("Stage8 Cup"),
            new CurrentTournamentUser(Guid.NewGuid(), "Player", "Active"));

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.AccessDenied, result.Error);
    }

    [Theory]
    [InlineData("-bad")]
    [InlineData("bad-")]
    [InlineData("bad  title")]
    [InlineData("bad--title")]
    public async Task InvalidTitle_GetsInvalidTitle(string title)
    {
        var service = CreateService(new InMemoryTournamentRepository());

        var result = await service.CreateAsync(ValidRequest(title), ActiveOrganizer);

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.InvalidTitle, result.Error);
    }

    [Fact]
    public async Task DuplicateTitle_GetsDuplicateTitle()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        await service.CreateAsync(ValidRequest("Stage8 Cup"), ActiveOrganizer);

        var result = await service.CreateAsync(ValidRequest("stage8 cup"), ActiveOrganizer);

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.DuplicateTitle, result.Error);
    }

    [Fact]
    public async Task MaxPlayersAboveLimit_GetsInvalidMaxPlayers()
    {
        var service = CreateService(new InMemoryTournamentRepository());

        var result = await service.CreateAsync(ValidRequest("Stage8 Cup") with { MaxPlayers = 1001 }, ActiveOrganizer);

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.InvalidMaxPlayers, result.Error);
    }

    [Fact]
    public async Task MaxPlayersNotMultipleOfTeamSize_GetsError()
    {
        var service = CreateService(new InMemoryTournamentRepository());

        var result = await service.CreateAsync(
            ValidRequest("Stage8 Cup") with { TeamSize = 5, MaxPlayers = 12 },
            ActiveOrganizer);

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.MaxPlayersNotMultipleOfTeamSize, result.Error);
    }

    [Fact]
    public async Task SwissWithoutRounds_GetsInvalidSwissRounds()
    {
        var service = CreateService(new InMemoryTournamentRepository());

        var result = await service.CreateAsync(
            ValidRequest("Stage8 Cup") with { Format = "Swiss", SwissRounds = null },
            ActiveOrganizer);

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.InvalidSwissRounds, result.Error);
    }

    [Fact]
    public async Task GetEndpoints_ReturnExpectedLists()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        await service.CreateAsync(ValidRequest("Stage8 Cup"), ActiveOrganizer);

        var all = await service.GetAllAsync();
        var available = await service.GetAvailableAsync();
        var organizerTournaments = await service.GetOrganizerTournamentsAsync(ActiveOrganizer);

        Assert.Single(all.Value);
        Assert.Single(available.Value);
        Assert.Single(organizerTournaments.Value);
        Assert.Empty((await service.GetActiveAsync()).Value);
        Assert.Empty((await service.GetCompletedAsync()).Value);
    }

    [Fact]
    public async Task GetMy_ReturnsPlayerTournaments()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        var tournament = await CreateTournamentAsync(service, "My Cup");
        var player = Player();
        await service.RegisterPlayerAsync(tournament.Id, player);

        var result = await service.GetMyAsync(player);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }

    [Fact]
    public async Task Player_CanRegisterToOpenTournament()
    {
        var repository = new InMemoryTournamentRepository();
        var outbox = new InMemoryOutboxWriter();
        var service = CreateService(repository, outbox);
        var tournament = await CreateTournamentAsync(service, "Registration Cup");
        var player = Player();

        var result = await service.RegisterPlayerAsync(tournament.Id, player);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.CurrentPlayersCount);
        Assert.Single(result.Value.Participants);
        Assert.Contains(outbox.Events, integrationEvent => integrationEvent is PlayerRegisteredToTournamentEvent);
    }

    [Fact]
    public async Task DuplicateRegistration_ReturnsConflictError()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        var tournament = await CreateTournamentAsync(service, "Duplicate Cup");
        var player = Player();
        await service.RegisterPlayerAsync(tournament.Id, player);

        var result = await service.RegisterPlayerAsync(tournament.Id, player);

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.DuplicateRegistration, result.Error);
    }

    [Theory]
    [InlineData("InProgress")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    public async Task RegistrationInNotOpenTournament_IsForbidden(string status)
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        var tournamentResponse = await CreateTournamentAsync(service, $"{status} Cup");
        var tournament = repository.Tournaments.Single(tournament => tournament.Id == tournamentResponse.Id);
        MoveToStatus(tournament, status);

        var result = await service.RegisterPlayerAsync(tournament.Id, Player());

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.TournamentRegistrationClosed, result.Error);
    }

    [Fact]
    public async Task ReachingMaxPlayers_StartsTournament()
    {
        var repository = new InMemoryTournamentRepository();
        var lifecycle = new TestTournamentLifecycleService();
        var service = CreateService(repository, lifecycleService: lifecycle);
        var tournament = await CreateTournamentAsync(service, "Auto Start Cup", maxPlayers: 1);

        var result = await service.RegisterPlayerAsync(tournament.Id, Player());

        Assert.True(result.IsSuccess);
        Assert.Equal(1, lifecycle.StartAttempts);
        Assert.Equal("Open", result.Value.Status);
        Assert.Equal(1, result.Value.CurrentPlayersCount);
    }

    [Fact]
    public async Task Player_CanLeaveBeforeStart()
    {
        var repository = new InMemoryTournamentRepository();
        var outbox = new InMemoryOutboxWriter();
        var service = CreateService(repository, outbox);
        var tournament = await CreateTournamentAsync(service, "Leave Cup", maxPlayers: 2);
        var player = Player();
        await service.RegisterPlayerAsync(tournament.Id, player);

        var result = await service.LeaveAsync(tournament.Id, player);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.CurrentPlayersCount);
        Assert.Contains(outbox.Events, integrationEvent => integrationEvent is PlayerLeftTournamentEvent);
    }

    [Fact]
    public async Task Player_CannotLeaveAfterStart()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        var tournamentResponse = await CreateTournamentAsync(service, "No Leave Cup", maxPlayers: 2);
        var player = Player();
        await service.RegisterPlayerAsync(tournamentResponse.Id, player);
        var tournament = repository.Tournaments.Single(tournament => tournament.Id == tournamentResponse.Id);
        tournament.Start(DateTime.UtcNow);

        var result = await service.LeaveAsync(tournament.Id, player);

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.TournamentAlreadyStarted, result.Error);
    }

    [Fact]
    public async Task Organizer_CanCancelOwnTournament()
    {
        var repository = new InMemoryTournamentRepository();
        var outbox = new InMemoryOutboxWriter();
        var service = CreateService(repository, outbox);
        var tournament = await CreateTournamentAsync(service, "Cancel Cup");

        var result = await service.CancelAsync(tournament.Id, ActiveOrganizer);

        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", result.Value.Status);
        Assert.Contains(outbox.Events, integrationEvent => integrationEvent is TournamentCancelledEvent);
    }

    [Fact]
    public async Task Organizer_CannotCancelForeignTournament()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        var tournament = await CreateTournamentAsync(service, "Foreign Cancel Cup");

        var result = await service.CancelAsync(tournament.Id, new CurrentTournamentUser(Guid.NewGuid(), "Organizer", "Active"));

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.AccessDenied, result.Error);
    }

    [Fact]
    public async Task Admin_CanCancelAnyTournament()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        var tournament = await CreateTournamentAsync(service, "Admin Cancel Cup");

        var result = await service.CancelAsync(tournament.Id, new CurrentTournamentUser(Guid.NewGuid(), "Admin", "Active"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Cancelled", result.Value.Status);
    }

    [Fact]
    public async Task CancelCompletedTournament_ReturnsConflictError()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        var tournamentResponse = await CreateTournamentAsync(service, "Completed Cancel Cup");
        var tournament = repository.Tournaments.Single(tournament => tournament.Id == tournamentResponse.Id);
        tournament.Complete(DateTime.UtcNow);

        var result = await service.CancelAsync(tournament.Id, ActiveOrganizer);

        Assert.True(result.IsFailure);
        Assert.Equal(TournamentErrors.CannotCancelCompleted, result.Error);
    }

    [Fact]
    public async Task CancelAlreadyCancelledTournament_IsIdempotent()
    {
        var repository = new InMemoryTournamentRepository();
        var outbox = new InMemoryOutboxWriter();
        var service = CreateService(repository, outbox);
        var tournament = await CreateTournamentAsync(service, "Idempotent Cancel Cup");
        await service.CancelAsync(tournament.Id, ActiveOrganizer);

        var result = await service.CancelAsync(tournament.Id, ActiveOrganizer);

        Assert.True(result.IsSuccess);
        Assert.Single(outbox.Events.OfType<TournamentCancelledEvent>());
    }

    [Fact]
    public async Task AdminDelete_SoftDeletesTournament()
    {
        var repository = new InMemoryTournamentRepository();
        var service = CreateService(repository);
        var tournamentResponse = await CreateTournamentAsync(service, "Delete Cup");

        var result = await service.DeleteAsync(tournamentResponse.Id, new CurrentTournamentUser(Guid.NewGuid(), "Admin", "Active"));

        Assert.True(result.IsSuccess);
        Assert.True(repository.Tournaments.Single().IsDeleted);
    }

    private static CreateTournamentRequest ValidRequest(string title)
    {
        return new CreateTournamentRequest(
            title,
            "Description",
            DisciplineCodes.CS2,
            "SingleElimination",
            null,
            1,
            16);
    }

    private static TournamentService CreateService(
        InMemoryTournamentRepository repository,
        InMemoryOutboxWriter? outboxWriter = null,
        ITournamentLifecycleService? lifecycleService = null)
    {
        return new TournamentService(
            repository,
            outboxWriter ?? new InMemoryOutboxWriter(),
            lifecycleService ?? new TestTournamentLifecycleService());
    }

    private static async Task<TournamentDetailsResponse> CreateTournamentAsync(
        TournamentService service,
        string title,
        int maxPlayers = 16)
    {
        var result = await service.CreateAsync(ValidRequest(title) with { MaxPlayers = maxPlayers }, ActiveOrganizer);
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static CurrentTournamentUser Player()
    {
        return new CurrentTournamentUser(Guid.NewGuid(), "Player", "Active", "PlayerOne");
    }

    private static void MoveToStatus(Domain.Tournaments.Tournament tournament, string status)
    {
        if (status == "InProgress")
        {
            tournament.Start(DateTime.UtcNow);
            return;
        }

        if (status == "Completed")
        {
            tournament.Complete(DateTime.UtcNow);
            return;
        }

        tournament.Cancel(DateTime.UtcNow);
    }

    private sealed class InMemoryTournamentRepository : ITournamentRepository
    {
        private readonly List<Discipline> _disciplines =
        [
            Discipline.Create(DisciplineCodes.CS2, "CS2", [1, 2, 5])
        ];

        public List<Domain.Tournaments.Tournament> Tournaments { get; } = [];

        public Task<bool> TitleExistsAsync(string normalizedTitle, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Tournaments.Any(tournament => tournament.NormalizedTitle == normalizedTitle));
        }

        public Task<Discipline?> GetActiveDisciplineAsync(
            string disciplineCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_disciplines.FirstOrDefault(discipline =>
                discipline.Code == disciplineCode && discipline.IsActive));
        }

        public Task<Domain.Tournaments.Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Tournaments.FirstOrDefault(tournament => tournament.Id == id));
        }

        public Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Domain.Tournaments.Tournament>>(Tournaments.ToArray());
        }

        public Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetByStatusAsync(
            TournamentStatus status,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Domain.Tournaments.Tournament>>(
                Tournaments.Where(tournament => tournament.Status == status).ToArray());
        }

        public Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetAvailableAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Domain.Tournaments.Tournament>>(
                Tournaments.Where(tournament => tournament.Status == TournamentStatus.Open).ToArray());
        }

        public Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetByOrganizerAsync(
            Guid organizerId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Domain.Tournaments.Tournament>>(
                Tournaments.Where(tournament => tournament.OrganizerId == organizerId).ToArray());
        }

        public Task<IReadOnlyCollection<Domain.Tournaments.Tournament>> GetByPlayerAsync(
            Guid playerId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<Domain.Tournaments.Tournament>>(
                Tournaments.Where(tournament => tournament.Participants.Any(participant =>
                    participant.PlayerId == playerId && (participant.IsActive || tournament.StartedAtUtc is not null))).ToArray());
        }

        public Task<bool> DeletedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public void Add(Domain.Tournaments.Tournament tournament)
        {
            Tournaments.Add(tournament);
        }

        public Task<ITournamentTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ITournamentTransaction>(new NoopTournamentTransaction());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class NoopTournamentTransaction : ITournamentTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class InMemoryOutboxWriter : IOutboxWriter
    {
        public List<IntegrationEvent> Events { get; } = [];

        public void Add(IntegrationEvent integrationEvent)
        {
            Events.Add(integrationEvent);
        }
    }

    private sealed class TestTournamentLifecycleService : ITournamentLifecycleService
    {
        public int StartAttempts { get; private set; }

        public Task TryStartTournamentAsync(Guid tournamentId, CancellationToken cancellationToken = default)
        {
            StartAttempts++;
            return Task.CompletedTask;
        }
    }
}
