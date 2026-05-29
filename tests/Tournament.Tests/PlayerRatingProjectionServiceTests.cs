using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Services;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Common;
using TournamentPlatform.Contracts.Events;

namespace Tournament.Tests;

public sealed class PlayerRatingProjectionServiceTests
{
    [Fact]
    public async Task UserCreatedPlayer_CreatesInitialProjections()
    {
        var repository = new InMemoryProjectionRepository();
        var service = new PlayerRatingProjectionService(repository);
        var userId = Guid.NewGuid();

        await service.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = userId,
            Role = "Player",
            Email = "player@test.local",
            CreatedAtUtc = DateTime.UtcNow,
            CreationSource = "Test"
        });

        Assert.Equal(3, repository.Projections.Count);
        Assert.All(repository.Projections, projection => Assert.Equal(1000, projection.Elo));
    }

    [Fact]
    public async Task UserCreatedOrganizer_IsIgnored()
    {
        var repository = new InMemoryProjectionRepository();
        var service = new PlayerRatingProjectionService(repository);

        await service.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Role = "Organizer",
            Email = "organizer@test.local",
            CreatedAtUtc = DateTime.UtcNow,
            CreationSource = "Test"
        });

        Assert.Empty(repository.Projections);
    }

    [Fact]
    public async Task UserCreatedPlayer_IsIdempotent()
    {
        var repository = new InMemoryProjectionRepository();
        var service = new PlayerRatingProjectionService(repository);
        var integrationEvent = new UserCreatedEvent
        {
            UserId = Guid.NewGuid(),
            Role = "Player",
            Email = "player@test.local",
            CreatedAtUtc = DateTime.UtcNow,
            CreationSource = "Test"
        };

        await service.HandleUserCreatedAsync(integrationEvent);
        await service.HandleUserCreatedAsync(integrationEvent);

        Assert.Equal(3, repository.Projections.Count);
    }

    [Fact]
    public async Task RatingUpdated_UpdatesExistingProjection()
    {
        var repository = new InMemoryProjectionRepository();
        var service = new PlayerRatingProjectionService(repository);
        var userId = Guid.NewGuid();
        await service.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = userId,
            Role = "Player",
            Email = "player@test.local",
            CreatedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            CreationSource = "Test"
        });

        await service.HandleRatingUpdatedAsync(new RatingUpdatedEvent
        {
            UserId = userId,
            DisciplineCode = DisciplineCodes.CS2,
            OldElo = 1000,
            NewElo = 1130,
            UpdatedAtUtc = DateTime.UtcNow
        });

        var projection = repository.Projections.Single(projection => projection.DisciplineCode == DisciplineCodes.CS2);
        Assert.Equal(1130, projection.Elo);
        Assert.Equal(3, repository.Projections.Count);
    }

    [Fact]
    public async Task RatingUpdated_CreatesMissingProjection_AndIsIdempotent()
    {
        var repository = new InMemoryProjectionRepository();
        var service = new PlayerRatingProjectionService(repository);
        var integrationEvent = new RatingUpdatedEvent
        {
            UserId = Guid.NewGuid(),
            DisciplineCode = DisciplineCodes.Valorant,
            OldElo = 1000,
            NewElo = 1042,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await service.HandleRatingUpdatedAsync(integrationEvent);
        await service.HandleRatingUpdatedAsync(integrationEvent);

        Assert.Single(repository.Projections);
        Assert.Equal(1042, repository.Projections.Single().Elo);
    }

    [Fact]
    public async Task UserDeleted_CreatesDeletedProjection_Idempotently()
    {
        var repository = new InMemoryProjectionRepository();
        var service = new PlayerRatingProjectionService(repository);
        var integrationEvent = new UserDeletedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "player@test.local",
            DeletedAtUtc = DateTime.UtcNow
        };

        await service.HandleUserDeletedAsync(integrationEvent);
        await service.HandleUserDeletedAsync(integrationEvent);

        Assert.Single(repository.DeletedUsers);
    }

    private sealed class InMemoryProjectionRepository : IPlayerRatingProjectionRepository
    {
        public List<PlayerRatingProjection> Projections { get; } = [];
        public List<DeletedUserProjection> DeletedUsers { get; } = [];

        public Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdAsync(
            Guid playerId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PlayerRatingProjection>>(
                Projections.Where(projection => projection.PlayerId == playerId).ToArray());
        }

        public Task<PlayerRatingProjection?> GetAsync(
            Guid playerId,
            string disciplineCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Projections.FirstOrDefault(projection =>
                projection.PlayerId == playerId && projection.DisciplineCode == disciplineCode));
        }

        public Task<IReadOnlyCollection<PlayerRatingProjection>> GetByPlayerIdsAsync(
            IReadOnlyCollection<Guid> playerIds,
            string disciplineCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<PlayerRatingProjection>>(
                Projections
                    .Where(projection => playerIds.Contains(projection.PlayerId)
                        && projection.DisciplineCode == disciplineCode)
                    .ToArray());
        }

        public Task<bool> DeletedUserExistsAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DeletedUsers.Any(projection => projection.UserId == userId));
        }

        public Task AddDeletedUserAsync(
            Guid userId,
            DateTime deletedAtUtc,
            CancellationToken cancellationToken = default)
        {
            DeletedUsers.Add(DeletedUserProjection.Create(userId, deletedAtUtc));
            return Task.CompletedTask;
        }

        public void Add(PlayerRatingProjection projection)
        {
            if (Projections.All(existing =>
                    existing.PlayerId != projection.PlayerId
                    || existing.DisciplineCode != projection.DisciplineCode))
            {
                Projections.Add(projection);
            }
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
