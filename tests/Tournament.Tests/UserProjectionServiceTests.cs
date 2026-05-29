using Tournament.Application.Tournaments.Abstractions;
using Tournament.Application.Tournaments.Services;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Contracts.Events;

namespace Tournament.Tests;

public sealed class UserProjectionServiceTests
{
    [Fact]
    public async Task HandleUserCreated_StoresContactHandleFromEventPayload()
    {
        var repository = new InMemoryUserProjectionRepository();
        var service = new UserProjectionService(repository);
        var userId = Guid.NewGuid();

        await service.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = userId,
            Role = "Player",
            Email = "p@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            CreationSource = "Registration",
            PlayerNickname = "PlayerOne",
            ContactHandle = "@player_one"
        });

        var projection = Assert.Single(repository.Users);
        Assert.Equal(userId, projection.UserId);
        Assert.Equal("@player_one", projection.ContactHandle);
    }

    [Fact]
    public async Task HandleUserContactHandleChanged_UpdatesExistingProjection()
    {
        var userId = Guid.NewGuid();
        var existing = UserProjection.Create(userId, "Player", "@old", DateTime.UtcNow);
        var repository = new InMemoryUserProjectionRepository();
        repository.Users.Add(existing);
        var service = new UserProjectionService(repository);

        await service.HandleUserContactHandleChangedAsync(new UserContactHandleChangedEvent
        {
            UserId = userId,
            ContactHandle = "@new",
            ChangedAtUtc = DateTime.UtcNow
        });

        Assert.Equal("@new", existing.ContactHandle);
    }

    [Fact]
    public async Task HandleUserContactHandleChanged_NoOpForUnknownUser()
    {
        var repository = new InMemoryUserProjectionRepository();
        var service = new UserProjectionService(repository);

        await service.HandleUserContactHandleChangedAsync(new UserContactHandleChangedEvent
        {
            UserId = Guid.NewGuid(),
            ContactHandle = "@new",
            ChangedAtUtc = DateTime.UtcNow
        });

        Assert.Empty(repository.Users);
    }

    [Fact]
    public async Task HandleUserCreated_RestoresAndUpdatesContactForExistingProjection()
    {
        var userId = Guid.NewGuid();
        var existing = UserProjection.Create(userId, "Player", "@old", DateTime.UtcNow);
        existing.MarkDeleted(DateTime.UtcNow);
        var repository = new InMemoryUserProjectionRepository();
        repository.Users.Add(existing);
        var service = new UserProjectionService(repository);

        await service.HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = userId,
            Role = "Player",
            Email = "p@example.com",
            CreatedAtUtc = DateTime.UtcNow,
            CreationSource = "Registration",
            PlayerNickname = "PlayerOne",
            ContactHandle = "@new"
        });

        Assert.False(existing.IsDeleted);
        Assert.Equal("@new", existing.ContactHandle);
    }

    private sealed class InMemoryUserProjectionRepository : IUserProjectionRepository
    {
        public List<UserProjection> Users { get; } = [];

        public Task<UserProjection?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.FirstOrDefault(u => u.UserId == userId));

        public Task<IReadOnlyCollection<UserProjection>> GetByIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<UserProjection>>(
                Users.Where(u => userIds.Contains(u.UserId)).ToArray());

        public void Add(UserProjection projection) => Users.Add(projection);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
