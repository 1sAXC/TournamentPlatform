using Rating.Application.Ratings.Abstractions;
using Rating.Application.Ratings.Services;
using Rating.Domain.Ratings;
using TournamentPlatform.Contracts.Common;
using TournamentPlatform.Contracts.Events;

namespace Rating.Tests;

public sealed class RatingServiceTests
{
    [Fact]
    public async Task HandleUserCreatedAsync_ShouldCreateFourInitialRatingsForPlayer()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var playerId = Guid.NewGuid();

        await service.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = playerId,
            Role = "Player",
            Email = "player@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            CreationSource = "Registration",
            PlayerNickname = "PlayerOne"
        });

        Assert.Equal(3, repository.PlayerRatings.Count);
        Assert.All(repository.PlayerRatings, rating =>
        {
            Assert.Equal(playerId, rating.PlayerId);
            Assert.Equal(1000, rating.Elo);
            Assert.False(rating.IsDeleted);
        });
        Assert.Contains(repository.PlayerRatings, rating => rating.DisciplineCode == DisciplineCodes.CS2);
        Assert.Contains(repository.PlayerRatings, rating => rating.DisciplineCode == DisciplineCodes.Valorant);
        Assert.Contains(repository.PlayerRatings, rating => rating.DisciplineCode == DisciplineCodes.Standoff2);
    }

    [Fact]
    public async Task HandleUserCreatedAsync_ShouldIgnoreOrganizer()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);

        await service.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Role = "Organizer",
            Email = "organizer@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            CreationSource = "Approval",
            OrganizerName = "Organizer Inc"
        });

        Assert.Empty(repository.PlayerRatings);
        Assert.Empty(repository.RatingHistories);
    }

    [Fact]
    public async Task HandleUserCreatedAsync_ShouldBeIdempotent()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var playerId = Guid.NewGuid();
        var integrationEvent = new UserCreatedEvent
        {
            UserId = playerId,
            Role = "Player",
            Email = "player@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            CreationSource = "Registration",
            PlayerNickname = "PlayerOne"
        };

        await service.HandleUserCreatedAsync(integrationEvent);
        await service.HandleUserCreatedAsync(integrationEvent);

        Assert.Equal(3, repository.PlayerRatings.Count);
        Assert.Equal(3, repository.RatingHistories.Count);
    }

    [Fact]
    public async Task HandleUserBlockedAsync_ShouldMarkRatingsDeleted()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var playerId = Guid.NewGuid();
        await service.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = playerId,
            Role = "Player",
            Email = "player@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            CreationSource = "Registration",
            PlayerNickname = "PlayerOne"
        });

        await service.HandleUserBlockedAsync(new UserBlockedEvent
        {
            UserId = playerId,
            Email = "player@example.com",
            BlockedAtUtc = DateTime.UtcNow
        });

        Assert.All(repository.PlayerRatings, rating => Assert.True(rating.IsDeleted));
        Assert.Equal(3, repository.RatingHistories.Count);
    }

    [Fact]
    public async Task HandleUserRoleChangedAsync_ToPlayer_ShouldCreateInitialRatings()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var playerId = Guid.NewGuid();

        await service.HandleUserRoleChangedAsync(new UserRoleChangedEvent
        {
            UserId = playerId,
            OldRole = "Organizer",
            NewRole = "Player",
            Nickname = "PlayerOne",
            ChangedAtUtc = DateTime.UtcNow
        });

        Assert.Equal(3, repository.PlayerRatings.Count);
        Assert.Equal(3, repository.RatingHistories.Count);
        Assert.All(repository.PlayerRatings, rating =>
        {
            Assert.Equal(playerId, rating.PlayerId);
            Assert.Equal(1000, rating.Elo);
            Assert.False(rating.IsDeleted);
        });
    }

    [Fact]
    public async Task HandleUserRoleChangedAsync_ToPlayer_ShouldBeIdempotent()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var integrationEvent = new UserRoleChangedEvent
        {
            UserId = Guid.NewGuid(),
            OldRole = "Organizer",
            NewRole = "Player",
            Nickname = "PlayerOne",
            ChangedAtUtc = DateTime.UtcNow
        };

        await service.HandleUserRoleChangedAsync(integrationEvent);
        await service.HandleUserRoleChangedAsync(integrationEvent);

        Assert.Equal(3, repository.PlayerRatings.Count);
        Assert.Equal(3, repository.RatingHistories.Count);
    }

    [Fact]
    public async Task HandleUserRoleChangedAsync_ToNonPlayer_ShouldNotCreateRatings()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);

        await service.HandleUserRoleChangedAsync(new UserRoleChangedEvent
        {
            UserId = Guid.NewGuid(),
            OldRole = "Player",
            NewRole = "Organizer",
            OrganizerName = "Organizer Inc",
            ChangedAtUtc = DateTime.UtcNow
        });

        Assert.Empty(repository.PlayerRatings);
        Assert.Empty(repository.RatingHistories);
    }

    [Fact]
    public async Task HandleMatchCompletedAsync_ShouldUpdateRatingsCreateHistoryAndPublishEvents()
    {
        var repository = new InMemoryRatingRepository();
        var outbox = new InMemoryOutboxWriter();
        var service = CreateService(repository, outbox);
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        repository.AddPlayerRating(PlayerRating.CreateInitial(winnerId, DisciplineCodes.CS2, 1000, DateTime.UtcNow));
        repository.AddPlayerRating(PlayerRating.CreateInitial(loserId, DisciplineCodes.CS2, 1000, DateTime.UtcNow));

        await service.HandleMatchCompletedAsync(MatchCompleted(winnerId, loserId));

        Assert.True(repository.PlayerRatings.Single(rating => rating.PlayerId == winnerId).Elo > 1000);
        Assert.True(repository.PlayerRatings.Single(rating => rating.PlayerId == loserId).Elo < 1000);
        Assert.Equal(2, repository.RatingHistories.Count);
        Assert.Equal(2, outbox.Events.OfType<RatingUpdatedEvent>().Count());
    }

    [Fact]
    public async Task HandleMatchCompletedAsync_TwoVsTwo_ShouldApplySameDeltaToAllPlayersInSameTeam()
    {
        var repository = new InMemoryRatingRepository();
        var outbox = new InMemoryOutboxWriter();
        var service = CreateService(repository, outbox);
        var winnerIds = NewPlayers(2);
        var loserIds = NewPlayers(2);
        AddRating(repository, winnerIds[0], 1600);
        AddRating(repository, winnerIds[1], 1400);
        AddRating(repository, loserIds[0], 1500);
        AddRating(repository, loserIds[1], 1500);

        await service.HandleMatchCompletedAsync(MatchCompleted(
            winnerIds,
            loserIds,
            teamSize: 2,
            winnerScore: null,
            loserScore: null));

        Assert.Equal(1614, GetRating(repository, winnerIds[0]).Elo);
        Assert.Equal(1414, GetRating(repository, winnerIds[1]).Elo);
        Assert.Equal(1486, GetRating(repository, loserIds[0]).Elo);
        Assert.Equal(1486, GetRating(repository, loserIds[1]).Elo);
        Assert.Equal(4, repository.RatingHistories.Count);
        Assert.Equal(4, outbox.Events.OfType<RatingUpdatedEvent>().Count());
    }

    [Fact]
    public async Task HandleMatchCompletedAsync_FiveVsFive_ShouldUseLowerKFactor()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var winnerIds = NewPlayers(5);
        var loserIds = NewPlayers(5);
        foreach (var playerId in winnerIds.Concat(loserIds))
        {
            AddRating(repository, playerId, 1000);
        }

        await service.HandleMatchCompletedAsync(MatchCompleted(
            winnerIds,
            loserIds,
            teamSize: 5,
            winnerScore: null,
            loserScore: null));

        Assert.All(winnerIds, playerId => Assert.Equal(1012, GetRating(repository, playerId).Elo));
        Assert.All(loserIds, playerId => Assert.Equal(988, GetRating(repository, playerId).Elo));
    }

    [Fact]
    public async Task HandleMatchCompletedAsync_ShouldUseScoreMultiplier_WhenScoreDifferenceExists()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        AddRating(repository, winnerId, 1000);
        AddRating(repository, loserId, 1000);

        await service.HandleMatchCompletedAsync(MatchCompleted(
            [winnerId],
            [loserId],
            teamSize: 1,
            winnerScore: 10,
            loserScore: 0));

        Assert.Equal(1020, GetRating(repository, winnerId).Elo);
        Assert.Equal(980, GetRating(repository, loserId).Elo);
    }

    [Fact]
    public async Task HandleMatchCompletedAsync_TechnicalDefeat_ShouldUseMaxScoreMultiplier()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        AddRating(repository, winnerId, 1000);
        AddRating(repository, loserId, 1000);

        await service.HandleMatchCompletedAsync(MatchCompleted(
            [winnerId],
            [loserId],
            teamSize: 1,
            winnerScore: null,
            loserScore: null,
            isTechnicalDefeat: true));

        Assert.Equal(1020, GetRating(repository, winnerId).Elo);
        Assert.Equal(980, GetRating(repository, loserId).Elo);
    }

    [Fact]
    public async Task HandleMatchCompletedAsync_ShouldBeIdempotent()
    {
        var repository = new InMemoryRatingRepository();
        var outbox = new InMemoryOutboxWriter();
        var service = CreateService(repository, outbox);
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        repository.AddPlayerRating(PlayerRating.CreateInitial(winnerId, DisciplineCodes.CS2, 1000, DateTime.UtcNow));
        repository.AddPlayerRating(PlayerRating.CreateInitial(loserId, DisciplineCodes.CS2, 1000, DateTime.UtcNow));
        var integrationEvent = MatchCompleted(winnerId, loserId);

        await service.HandleMatchCompletedAsync(integrationEvent);
        var winnerElo = repository.PlayerRatings.Single(rating => rating.PlayerId == winnerId).Elo;
        await service.HandleMatchCompletedAsync(integrationEvent);

        Assert.Equal(winnerElo, repository.PlayerRatings.Single(rating => rating.PlayerId == winnerId).Elo);
        Assert.Equal(2, repository.RatingHistories.Count);
        Assert.Equal(2, outbox.Events.OfType<RatingUpdatedEvent>().Count());
    }

    [Fact]
    public async Task HandleMatchCompletedAsync_ShouldNotDropBelowMinimumElo()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        AddRating(repository, winnerId, 100);
        AddRating(repository, loserId, 105);

        await service.HandleMatchCompletedAsync(MatchCompleted(
            [winnerId],
            [loserId],
            teamSize: 1,
            winnerScore: null,
            loserScore: null));

        Assert.Equal(100, GetRating(repository, loserId).Elo);
        Assert.Equal(-5, repository.RatingHistories.Single(history => history.PlayerId == loserId).Delta);
    }

    [Fact]
    public async Task HandleMatchCompletedAsync_ShouldCreateMissingRatingWithInitialElo()
    {
        var repository = new InMemoryRatingRepository();
        var service = CreateService(repository);
        var winnerId = Guid.NewGuid();
        var loserId = Guid.NewGuid();
        AddRating(repository, loserId, 1000);

        await service.HandleMatchCompletedAsync(MatchCompleted(
            [winnerId],
            [loserId],
            teamSize: 1,
            winnerScore: null,
            loserScore: null));

        var winnerRating = GetRating(repository, winnerId);
        var winnerHistory = repository.RatingHistories.Single(history => history.PlayerId == winnerId);
        Assert.Equal(1016, winnerRating.Elo);
        Assert.Equal(1000, winnerHistory.OldElo);
        Assert.Equal(1016, winnerHistory.NewElo);
    }

    [Fact]
    public void EloCalculator_EqualRatings_OneVsOne_ShouldReturnPlus16WithoutScoreMultiplier()
    {
        var calculator = new EloCalculator();

        var delta = calculator.CalculateTeamDelta(
            teamAverageElo: 1000,
            opponentAverageElo: 1000,
            teamSize: 1,
            actualScore: 1,
            scoreMultiplier: 1);

        Assert.Equal(16, delta);
    }

    [Fact]
    public void EloCalculator_UnderdogWin_ShouldGiveBiggerDeltaThanFavoriteWin()
    {
        var calculator = new EloCalculator();

        var favoriteDelta = calculator.CalculateTeamDelta(1200, 900, 1, 1, 1);
        var underdogDelta = calculator.CalculateTeamDelta(900, 1200, 1, 1, 1);

        Assert.True(underdogDelta > favoriteDelta);
    }

    private static RatingService CreateService(
        InMemoryRatingRepository repository,
        InMemoryOutboxWriter? outbox = null)
    {
        return new RatingService(repository, new EloCalculator(), outbox ?? new InMemoryOutboxWriter());
    }

    private static Guid[] NewPlayers(int count)
    {
        return Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToArray();
    }

    private static void AddRating(InMemoryRatingRepository repository, Guid playerId, int elo)
    {
        repository.AddPlayerRating(PlayerRating.CreateInitial(playerId, DisciplineCodes.CS2, elo, DateTime.UtcNow));
    }

    private static PlayerRating GetRating(InMemoryRatingRepository repository, Guid playerId)
    {
        return repository.PlayerRatings.Single(rating => rating.PlayerId == playerId);
    }

    private static MatchCompletedEvent MatchCompleted(Guid winnerId, Guid loserId)
    {
        return MatchCompleted([winnerId], [loserId], teamSize: 1, winnerScore: 2, loserScore: 0);
    }

    private static MatchCompletedEvent MatchCompleted(
        IReadOnlyCollection<Guid> winnerIds,
        IReadOnlyCollection<Guid> loserIds,
        int teamSize,
        int? winnerScore,
        int? loserScore,
        bool isTechnicalDefeat = false)
    {
        return new MatchCompletedEvent
        {
            MatchId = Guid.NewGuid(),
            TournamentId = Guid.NewGuid(),
            DisciplineCode = DisciplineCodes.CS2,
            TeamSize = teamSize,
            WinnerTeamId = Guid.NewGuid(),
            LoserTeamId = Guid.NewGuid(),
            WinnerScore = winnerScore,
            LoserScore = loserScore,
            IsTechnicalDefeat = isTechnicalDefeat,
            WinnerPlayers = winnerIds
                .Select(playerId => new MatchCompletedPlayerDto
                {
                    UserId = playerId,
                    Nickname = "Winner",
                    EloBeforeMatch = 1000
                })
                .ToArray(),
            LoserPlayers = loserIds
                .Select(playerId => new MatchCompletedPlayerDto
                {
                    UserId = playerId,
                    Nickname = "Loser",
                    EloBeforeMatch = 1000
                })
                .ToArray()
        };
    }

    private sealed class InMemoryRatingRepository : IRatingRepository
    {
        public List<PlayerRating> PlayerRatings { get; } = [];
        public List<RatingHistory> RatingHistories { get; } = [];

        public Task<IReadOnlyCollection<PlayerRating>> GetPlayerRatingsAsync(
            Guid playerId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PlayerRating>>(PlayerRatings
                .Where(rating => rating.PlayerId == playerId && !rating.IsDeleted)
                .ToArray());
        }

        public Task<IReadOnlyCollection<PlayerRating>> GetAllPlayerRatingsAsync(
            Guid playerId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PlayerRating>>(PlayerRatings
                .Where(rating => rating.PlayerId == playerId)
                .ToArray());
        }

        public Task<PlayerRating?> GetPlayerRatingIncludingDeletedAsync(
            Guid playerId,
            string disciplineCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PlayerRatings.FirstOrDefault(rating =>
                rating.PlayerId == playerId
                && rating.DisciplineCode == disciplineCode));
        }

        public Task<IReadOnlyCollection<RatingHistory>> GetPlayerHistoryAsync(
            Guid playerId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<RatingHistory>>(RatingHistories
                .Where(history => history.PlayerId == playerId)
                .ToArray());
        }

        public Task<bool> HasAnyRatingAsync(Guid playerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PlayerRatings.Any(rating => rating.PlayerId == playerId));
        }

        public Task<bool> HasMatchHistoryAsync(Guid matchId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(RatingHistories.Any(history => history.MatchId == matchId));
        }

        public void AddPlayerRating(PlayerRating rating)
        {
            PlayerRatings.Add(rating);
        }

        public void AddRatingHistory(RatingHistory history)
        {
            RatingHistories.Add(history);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryOutboxWriter : Rating.Application.Ratings.Abstractions.IOutboxWriter
    {
        public List<IntegrationEvent> Events { get; } = [];

        public void Add(IntegrationEvent integrationEvent)
        {
            Events.Add(integrationEvent);
        }
    }
}
