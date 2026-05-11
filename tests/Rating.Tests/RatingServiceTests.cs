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

        Assert.Equal(4, repository.PlayerRatings.Count);
        Assert.All(repository.PlayerRatings, rating =>
        {
            Assert.Equal(playerId, rating.PlayerId);
            Assert.Equal(1000, rating.Elo);
            Assert.False(rating.IsDeleted);
        });
        Assert.Contains(repository.PlayerRatings, rating => rating.DisciplineCode == DisciplineCodes.CS2);
        Assert.Contains(repository.PlayerRatings, rating => rating.DisciplineCode == DisciplineCodes.PUBG);
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

        Assert.Equal(4, repository.PlayerRatings.Count);
        Assert.Equal(4, repository.RatingHistories.Count);
    }

    [Fact]
    public async Task HandleUserDeletedAsync_ShouldMarkRatingsDeleted()
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

        await service.HandleUserDeletedAsync(new UserDeletedEvent
        {
            UserId = playerId,
            Email = "player@example.com",
            DeletedAtUtc = DateTime.UtcNow
        });

        Assert.All(repository.PlayerRatings, rating => Assert.True(rating.IsDeleted));
        Assert.Equal(4, repository.RatingHistories.Count);
    }

    [Fact]
    public async Task GetPlayerRatingAsync_ShouldReturnNotFoundForMissingRating()
    {
        var service = CreateService(new InMemoryRatingRepository());

        var result = await service.GetPlayerRatingAsync(Guid.NewGuid(), DisciplineCodes.CS2);

        Assert.True(result.IsFailure);
        Assert.Equal(Rating.Application.Ratings.RatingErrors.RatingNotFound, result.Error);
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
    public void EloCalculator_ShouldGiveBiggerDeltaForUpset()
    {
        var calculator = new EloCalculator();

        var favoriteDelta = calculator.CalculateDelta(1200, 900, 1, 1);
        var underdogDelta = calculator.CalculateDelta(900, 1200, 1, 1);

        Assert.True(underdogDelta > favoriteDelta);
    }

    private static RatingService CreateService(
        InMemoryRatingRepository repository,
        InMemoryOutboxWriter? outbox = null)
    {
        return new RatingService(repository, new EloCalculator(), outbox ?? new InMemoryOutboxWriter());
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
            WinnerScore = 2,
            LoserScore = 0,
            WinnerPlayers =
            [
                new MatchCompletedPlayerDto
                {
                    UserId = winnerId,
                    Nickname = "Winner",
                    EloBeforeMatch = 1000
                }
            ],
            LoserPlayers =
            [
                new MatchCompletedPlayerDto
                {
                    UserId = loserId,
                    Nickname = "Loser",
                    EloBeforeMatch = 1000
                }
            ]
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

        public Task<PlayerRating?> GetPlayerRatingAsync(
            Guid playerId,
            string disciplineCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PlayerRatings.FirstOrDefault(rating =>
                rating.PlayerId == playerId
                && rating.DisciplineCode == disciplineCode
                && !rating.IsDeleted));
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
