using Auth.Application.Admin;
using Auth.Application.Admin.Dto;
using Auth.Application.Admin.Services;
using Auth.Application.Auth.Abstractions;
using Auth.Domain.Users;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Auth.Tests;

public sealed class AdminUsersServiceTests
{
    [Fact]
    public async Task ApproveOrganizer_ShouldActivatePendingOrganizerAndWriteUserCreated()
    {
        var repository = new InMemoryAuthUserRepository();
        var outbox = new InMemoryOutboxWriter();
        var organizer = User.CreateOrganizerSelfRegistration(
            "organizer@example.com",
            "hash",
            "Organizer Inc",
            DateTime.UtcNow);
        repository.Users.Add(organizer);

        var service = CreateService(repository, outbox);
        var result = await service.ApproveOrganizerAsync(organizer.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Active, organizer.Status);
        Assert.NotNull(organizer.ApprovedAtUtc);
        Assert.Single(outbox.Events);
        Assert.IsType<UserCreatedEvent>(outbox.Events[0]);
    }

    [Fact]
    public async Task RejectOrganizer_ShouldRejectPendingOrganizerWithoutUserCreated()
    {
        var repository = new InMemoryAuthUserRepository();
        var outbox = new InMemoryOutboxWriter();
        var organizer = User.CreateOrganizerSelfRegistration(
            "organizer@example.com",
            "hash",
            "Organizer Inc",
            DateTime.UtcNow);
        repository.Users.Add(organizer);

        var service = CreateService(repository, outbox);
        var result = await service.RejectOrganizerAsync(organizer.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Rejected, organizer.Status);
        Assert.NotNull(organizer.RejectedAtUtc);
        Assert.Empty(outbox.Events);
    }

    [Fact]
    public async Task DeleteUser_ShouldSoftDeleteAndWriteUserDeleted()
    {
        var repository = new InMemoryAuthUserRepository();
        var outbox = new InMemoryOutboxWriter();
        var admin = User.CreateAdmin(Guid.NewGuid(), "admin@example.com", "hash", DateTime.UtcNow);
        var player = User.CreatePlayer("player@example.com", "hash", "PlayerOne", DateTime.UtcNow);
        player.ClearDomainEvents();
        repository.Users.Add(admin);
        repository.Users.Add(player);

        var service = CreateService(repository, outbox);
        var result = await service.DeleteUserAsync(player.Id, admin.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountStatus.Deleted, player.Status);
        Assert.Single(outbox.Events);
        Assert.IsType<UserDeletedEvent>(outbox.Events[0]);
    }

    [Fact]
    public async Task DeleteUser_ShouldRejectDeletingSelfWhenLastActiveAdmin()
    {
        var repository = new InMemoryAuthUserRepository();
        var admin = User.CreateAdmin(Guid.NewGuid(), "admin@example.com", "hash", DateTime.UtcNow);
        repository.Users.Add(admin);

        var service = CreateService(repository, new InMemoryOutboxWriter());
        var result = await service.DeleteUserAsync(admin.Id, admin.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(AdminErrors.LastAdminDeleteNotAllowed, result.Error);
        Assert.Equal(AccountStatus.Active, admin.Status);
    }

    private static AdminUsersService CreateService(
        InMemoryAuthUserRepository repository,
        InMemoryOutboxWriter outbox)
    {
        return new AdminUsersService(repository, new StubPasswordHashingService(), outbox);
    }

    private sealed class InMemoryAuthUserRepository : IAuthUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.Any(user => user.NormalizedEmail == normalizedEmail));
        }

        public Task<bool> ExistsByNicknameAsync(string normalizedNickname, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.Any(user => user.NormalizedNickname == normalizedNickname));
        }

        public Task<User?> GetByLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.FirstOrDefault(user =>
                user.NormalizedEmail == normalizedLogin || user.NormalizedNickname == normalizedLogin));
        }

        public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.FirstOrDefault(user => user.Id == userId));
        }

        public Task<IReadOnlyCollection<User>> GetUsersAsync(
            int skip,
            int take,
            UserRole? role,
            AccountStatus? status,
            string? normalizedSearch,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<User>>(ApplyFilters(role, status, normalizedSearch)
                .Skip(skip)
                .Take(take)
                .ToArray());
        }

        public Task<int> CountUsersAsync(
            UserRole? role,
            AccountStatus? status,
            string? normalizedSearch,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ApplyFilters(role, status, normalizedSearch).Count());
        }

        public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.Count(user => user.Role == UserRole.Admin && user.Status == AccountStatus.Active));
        }

        public void Add(User user)
        {
            Users.Add(user);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        private IEnumerable<User> ApplyFilters(UserRole? role, AccountStatus? status, string? normalizedSearch)
        {
            var query = Users.AsEnumerable();

            if (role.HasValue)
            {
                query = query.Where(user => user.Role == role.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(user => user.Status == status.Value);
            }

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(user =>
                    user.NormalizedEmail.Contains(normalizedSearch)
                    || (user.NormalizedNickname?.Contains(normalizedSearch) ?? false)
                    || (user.NormalizedOrganizerName?.Contains(normalizedSearch) ?? false));
            }

            return query;
        }
    }

    private sealed class StubPasswordHashingService : IPasswordHashingService
    {
        public string HashPassword(User user, string password) => $"HASHED:{password}";

        public bool VerifyPassword(User user, string password) => user.PasswordHash == $"HASHED:{password}";
    }

    private sealed class InMemoryOutboxWriter : IOutboxWriter
    {
        public List<IntegrationEvent> Events { get; } = [];

        public void Add(IntegrationEvent integrationEvent)
        {
            Events.Add(integrationEvent);
        }
    }
}
