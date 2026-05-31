using Auth.Application.Auth.Abstractions;
using Auth.Application.Auth.Dto;
using Auth.Application.Auth.Services;
using Auth.Domain.Users;
using Rating.Application.Ratings.Services;
using Rating.Domain.Ratings;
using Tournament.Application.Brackets;
using Tournament.Application.TeamBalancer;
using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Dto;
using Tournament.Application.Tournaments.Services;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Common;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Integration.Tests;

public sealed class BusinessFlowTests
{
    [Fact]
    public async Task PlayerRegistration_UserCreated_CreatesRatingAndTournamentProjection()
    {
        var authOutbox = new AuthOutbox();
        var auth = new AuthService(new AuthUsers(), new PasswordHasher(), new JwtGenerator(), authOutbox);

        var result = await auth.RegisterPlayerAsync(new RegisterPlayerRequest("PlayerOne", "player@example.test", "Password1"));
        var userCreated = Assert.IsType<UserCreatedEvent>(Assert.Single(authOutbox.Events));

        var ratingRepository = new Ratings();
        await new RatingService(ratingRepository, new EloCalculator(), new RatingOutbox())
            .HandleUserCreatedAsync(userCreated);
        var projectionRepository = new Projections();
        await new PlayerRatingProjectionService(projectionRepository)
            .HandleUserCreatedAsync(userCreated);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, ratingRepository.PlayerRatings.Count);
        Assert.Equal(4, projectionRepository.Items.Count);
    }

    [Fact]
    public async Task FullTournament_CreatesTeamsBracketAndTournamentStarted()
    {
        var tournament = TournamentReadyToStart(players: 4, teamSize: 2);
        var outbox = new TournamentOutbox();
        var projections = new Projections();
        foreach (var participant in tournament.Participants)
        {
            projections.Add(PlayerRatingProjection.Create(participant.PlayerId, tournament.DisciplineCode, 1000, DateTime.UtcNow));
        }

        var lifecycle = new TournamentLifecycleService(
            projections,
            new GreedyTeamBalancer(new FirstRandom()),
            GeneratorFactory(outbox));

        await lifecycle.TryStartTournamentAsync(tournament);

        Assert.Equal(TournamentStatus.InProgress, tournament.Status);
        Assert.Equal(2, tournament.Teams.Count);
        Assert.Single(tournament.Rounds);
        Assert.Single(tournament.Rounds.Single().Matches);
    }

    [Fact]
    public async Task MatchCompleted_RatingUpdated_RefreshesTournamentProjection()
    {
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        var ratingRepository = new Ratings();
        ratingRepository.AddPlayerRating(PlayerRating.CreateInitial(winnerId, DisciplineCodes.CS2, 1000, DateTime.UtcNow));
        ratingRepository.AddPlayerRating(PlayerRating.CreateInitial(loserId, DisciplineCodes.CS2, 1000, DateTime.UtcNow));
        var ratingOutbox = new RatingOutbox();

        await new RatingService(ratingRepository, new EloCalculator(), ratingOutbox)
            .HandleMatchCompletedAsync(MatchCompleted(winnerId, loserId));

        var ratingUpdated = ratingOutbox.Events.OfType<RatingUpdatedEvent>()
            .Single(e => e.UserId == winnerId);
        var projectionRepository = new Projections();
        await new PlayerRatingProjectionService(projectionRepository)
            .HandleRatingUpdatedAsync(ratingUpdated);

        var projection = Assert.Single(projectionRepository.Items);
        Assert.Equal(winnerId, projection.PlayerId);
        Assert.Equal(ratingUpdated.NewElo, projection.Elo);
    }

    private static IBracketGeneratorFactory GeneratorFactory(TournamentOutbox outbox)
    {
        IBracketGenerator[] generators =
        [
            new SingleEliminationBracketGenerator(outbox),
            new DoubleEliminationBracketGenerator(outbox),
            new SwissBracketGenerator(outbox)
        ];
        return new BracketGeneratorFactory(generators);
    }

    private static Tournament.Domain.Tournaments.Tournament TournamentReadyToStart(int players, int teamSize)
    {
        var tournament = Tournament.Domain.Tournaments.Tournament.Create(
            "Integration Cup",
            "INTEGRATION CUP",
            null,
            DisciplineCodes.CS2,
            TournamentFormat.SingleElimination,
            null,
            teamSize,
            players,
            Guid.NewGuid(),
            DateTime.UtcNow);

        for (var i = 0; i < players; i++)
        {
            tournament.RegisterParticipant(Guid.NewGuid(), $"Player{i + 1}", DateTime.UtcNow.AddSeconds(i));
        }

        return tournament;
    }

    private static MatchCompletedEvent MatchCompleted(Guid winnerId, Guid loserId)
    {
        return new MatchCompletedEvent
        {
            MatchId = Guid.NewGuid(),
            TournamentId = Guid.NewGuid(),
            DisciplineCode = DisciplineCodes.CS2,
            TeamSize = 1,
            WinnerTeamId = Guid.NewGuid(),
            LoserTeamId = Guid.NewGuid(),
            WinnerPlayers = [new MatchCompletedPlayerDto { UserId = winnerId }],
            LoserPlayers = [new MatchCompletedPlayerDto { UserId = loserId }]
        };
    }

    private sealed class AuthUsers : IAuthUserRepository
    {
        private readonly List<User> _users = [];
        public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) => Task.FromResult(_users.Any(u => u.NormalizedEmail == normalizedEmail));
        public Task<bool> ExistsByNicknameAsync(string normalizedNickname, CancellationToken cancellationToken = default) => Task.FromResult(_users.Any(u => u.NormalizedNickname == normalizedNickname));
        public Task<User?> GetByLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default) => Task.FromResult(_users.FirstOrDefault(u => u.NormalizedEmail == normalizedLogin || u.NormalizedNickname == normalizedLogin));
        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(_users.FirstOrDefault(u => u.Id == userId));
        public Task<IReadOnlyCollection<User>> GetUsersAsync(int skip, int take, UserRole? role, AccountStatus? status, string? normalizedSearch, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<User>>(_users.Skip(skip).Take(take).ToArray());
        public Task<int> CountUsersAsync(UserRole? role, AccountStatus? status, string? normalizedSearch, CancellationToken cancellationToken = default) => Task.FromResult(_users.Count);
        public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default) => Task.FromResult(_users.Count(u => u.Role == UserRole.Admin && u.Status == AccountStatus.Active));
        public void Add(User user) => _users.Add(user);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class Ratings : Rating.Application.Ratings.Abstractions.IRatingRepository
    {
        public List<PlayerRating> PlayerRatings { get; } = [];
        public List<RatingHistory> Histories { get; } = [];
        public Task<IReadOnlyCollection<PlayerRating>> GetPlayerRatingsAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PlayerRating>>(PlayerRatings.Where(r => r.PlayerId == playerId && !r.IsDeleted).ToArray());
        public Task<IReadOnlyCollection<PlayerRating>> GetAllPlayerRatingsAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PlayerRating>>(PlayerRatings.Where(r => r.PlayerId == playerId).ToArray());
        public Task<PlayerRating?> GetPlayerRatingIncludingDeletedAsync(Guid playerId, string disciplineCode, CancellationToken cancellationToken = default) => Task.FromResult(PlayerRatings.FirstOrDefault(r => r.PlayerId == playerId && r.DisciplineCode == disciplineCode));
        public Task<IReadOnlyCollection<RatingHistory>> GetPlayerHistoryAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<RatingHistory>>(Histories.Where(h => h.PlayerId == playerId).ToArray());
        public Task<bool> HasAnyRatingAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult(PlayerRatings.Any(r => r.PlayerId == playerId));
        public Task<bool> HasMatchHistoryAsync(Guid matchId, CancellationToken cancellationToken = default) => Task.FromResult(Histories.Any(h => h.MatchId == matchId));
        public void AddPlayerRating(PlayerRating rating) => PlayerRatings.Add(rating);
        public void AddRatingHistory(RatingHistory history) => Histories.Add(history);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class Projections : IPlayerRatingProjectionRepository
    {
        public List<PlayerRatingProjection> Items { get; } = [];
        public Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PlayerRatingProjection>>(Items.Where(p => p.PlayerId == playerId).ToArray());
        public Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdsAsync(IReadOnlyCollection<Guid> playerIds, string disciplineCode, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<PlayerRatingProjection>>(Items.Where(p => playerIds.Contains(p.PlayerId) && p.DisciplineCode == disciplineCode).ToArray());
        public Task<PlayerRatingProjection?> GetAsync(Guid playerId, string disciplineCode, CancellationToken cancellationToken = default) => Task.FromResult(Items.FirstOrDefault(p => p.PlayerId == playerId && p.DisciplineCode == disciplineCode));
        public Task<bool> BlockedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddBlockedUserAsync(Guid userId, DateTime blockedAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveBlockedUserAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Add(PlayerRatingProjection projection) => Items.Add(projection);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class Tournaments(Tournament.Domain.Tournaments.Tournament tournament) : ITournamentRepository
    {
        public Task<bool> TitleExistsAsync(string normalizedTitle, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<Discipline?> GetActiveDisciplineAsync(string disciplineCode, CancellationToken cancellationToken = default) => Task.FromResult<Discipline?>(null);
        public Task<Tournament.Domain.Tournaments.Tournament?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(id == tournament.Id ? tournament : null);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([ToSummary(tournament)]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByStatusAsync(TournamentStatus status, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetAvailableAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<IReadOnlyCollection<TournamentSummaryDto>> GetByPlayerAsync(Guid playerId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyCollection<TournamentSummaryDto>>([]);
        public Task<bool> BlockedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<ITournamentTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public void Add(Tournament.Domain.Tournaments.Tournament value) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        private static TournamentSummaryDto ToSummary(Tournament.Domain.Tournaments.Tournament value)
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

    private sealed class PasswordHasher : IPasswordHashingService
    {
        public string HashPassword(User user, string password) => $"HASHED:{password}";
        public bool VerifyPassword(User user, string password) => user.PasswordHash == $"HASHED:{password}";
    }

    private sealed class JwtGenerator : IJwtTokenGenerator
    {
        public JwtToken Generate(User user) => new("token", DateTime.UtcNow.AddMinutes(30));
    }

    private sealed class FirstRandom : IRandomProvider
    {
        public int Next(int maxExclusive) => 0;
    }

    private sealed class AuthOutbox : Auth.Application.Auth.Abstractions.IOutboxWriter
    {
        public List<IntegrationEvent> Events { get; } = [];
        public void Add(IntegrationEvent integrationEvent) => Events.Add(integrationEvent);
    }

    private sealed class RatingOutbox : Rating.Application.Ratings.Abstractions.IOutboxWriter
    {
        public List<IntegrationEvent> Events { get; } = [];
        public void Add(IntegrationEvent integrationEvent) => Events.Add(integrationEvent);
    }

    private sealed class TournamentOutbox : Tournament.Application.Tournaments.Abstractions.IOutboxWriter
    {
        public List<IntegrationEvent> Events { get; } = [];
        public void Add(IntegrationEvent integrationEvent) => Events.Add(integrationEvent);
    }
}
