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
    public async Task GetOrganizerApplications_ShouldReturnOnlyPendingOrganizers()
    {
        var repository = new InMemoryAuthUserRepository();
        var pendingOrganizer = User.CreateOrganizerSelfRegistration(
            "pending@example.com",
            "hash",
            "Pending Org",
            DateTime.UtcNow.AddMinutes(-1));
        var approvedOrganizer = User.CreateOrganizerSelfRegistration(
            "approved@example.com",
            "hash",
            "Approved Org",
            DateTime.UtcNow);
        approvedOrganizer.Approve(DateTime.UtcNow);
        var player = User.CreatePlayer("player@example.com", "hash", "PlayerOne", DateTime.UtcNow);
        repository.Users.AddRange([pendingOrganizer, approvedOrganizer, player]);

        var service = CreateService(repository, new InMemoryOutboxWriter());
        var result = await service.GetOrganizerApplicationsAsync(new OrganizerApplicationsQuery());

        Assert.True(result.IsSuccess);
        var application = Assert.Single(result.Value.Items);
        Assert.Equal(pendingOrganizer.Id, application.Id);
        Assert.Equal("PendingApproval", application.Status);
        Assert.Equal("Pending Org", application.OrganizerName);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetOrganizerApplications_ShouldReturnEmptyPage_WhenNoPendingOrganizers()
    {
        var repository = new InMemoryAuthUserRepository();
        repository.Users.Add(User.CreatePlayer("player@example.com", "hash", "PlayerOne", DateTime.UtcNow));

        var service = CreateService(repository, new InMemoryOutboxWriter());
        var result = await service.GetOrganizerApplicationsAsync(new OrganizerApplicationsQuery());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task GetOrganizerApplicationsHistory_ShouldReturnApprovedAndRejected_ExcludingPendingAndAdminCreated()
    {
        var repository = new InMemoryAuthUserRepository();
        var pendingOrganizer = User.CreateOrganizerSelfRegistration(
            "pending@example.com",
            "hash",
            "Pending Org",
            DateTime.UtcNow.AddMinutes(-3));
        var approvedOrganizer = User.CreateOrganizerSelfRegistration(
            "approved@example.com",
            "hash",
            "Approved Org",
            DateTime.UtcNow.AddMinutes(-2));
        approvedOrganizer.Approve(DateTime.UtcNow);
        var rejectedOrganizer = User.CreateOrganizerSelfRegistration(
            "rejected@example.com",
            "hash",
            "Rejected Org",
            DateTime.UtcNow.AddMinutes(-1));
        rejectedOrganizer.Reject(DateTime.UtcNow);
        var adminCreatedOrganizer = User.CreateOrganizerByAdmin(
            "byadmin@example.com",
            "hash",
            "Admin-created Org",
            createdByAdminId: Guid.NewGuid(),
            DateTime.UtcNow);
        var player = User.CreatePlayer("player@example.com", "hash", "PlayerOne", DateTime.UtcNow);
        repository.Users.AddRange([pendingOrganizer, approvedOrganizer, rejectedOrganizer, adminCreatedOrganizer, player]);

        var service = CreateService(repository, new InMemoryOutboxWriter());
        var result = await service.GetOrganizerApplicationsHistoryAsync(new OrganizerApplicationsQuery());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Contains(result.Value.Items, a => a.Id == approvedOrganizer.Id && a.Status == "Active");
        Assert.Contains(result.Value.Items, a => a.Id == rejectedOrganizer.Id && a.Status == "Rejected");
        Assert.DoesNotContain(result.Value.Items, a => a.Id == pendingOrganizer.Id);
        Assert.DoesNotContain(result.Value.Items, a => a.Id == adminCreatedOrganizer.Id);
        Assert.DoesNotContain(result.Value.Items, a => a.Id == player.Id);
    }

    [Fact]
    public async Task GetOrganizerApplicationsHistory_ShouldReturnEmptyPage_WhenNoDecidedApplications()
    {
        var repository = new InMemoryAuthUserRepository();
        var pendingOrganizer = User.CreateOrganizerSelfRegistration(
            "pending@example.com",
            "hash",
            "Pending Org",
            DateTime.UtcNow);
        repository.Users.Add(pendingOrganizer);

        var service = CreateService(repository, new InMemoryOutboxWriter());
        var result = await service.GetOrganizerApplicationsHistoryAsync(new OrganizerApplicationsQuery());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task GetOrganizerApplication_ShouldReturnPendingOrganizer()
    {
        var repository = new InMemoryAuthUserRepository();
        var organizer = User.CreateOrganizerSelfRegistration(
            "organizer@example.com",
            "hash",
            "Organizer Inc",
            DateTime.UtcNow);
        repository.Users.Add(organizer);

        var service = CreateService(repository, new InMemoryOutboxWriter());
        var result = await service.GetOrganizerApplicationAsync(organizer.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(organizer.Id, result.Value.Id);
        Assert.Equal("organizer@example.com", result.Value.Email);
        Assert.Equal("Organizer Inc", result.Value.OrganizerName);
    }

    [Fact]
    public async Task GetOrganizerApplication_ShouldFail_WhenApplicationDoesNotExist()
    {
        var repository = new InMemoryAuthUserRepository();
        repository.Users.Add(User.CreatePlayer("player@example.com", "hash", "PlayerOne", DateTime.UtcNow));

        var service = CreateService(repository, new InMemoryOutboxWriter());
        var result = await service.GetOrganizerApplicationAsync(repository.Users.Single().Id);

        Assert.True(result.IsFailure);
        Assert.Equal(AdminErrors.UserNotFound, result.Error);
    }

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
    public async Task ApproveOrganizerApplication_ShouldReuseApproveLogic()
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
        var result = await service.ApproveOrganizerApplicationAsync(organizer.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(AccountStatus.Active, organizer.Status);
        Assert.Single(outbox.Events);
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
    public async Task RejectOrganizerApplication_ShouldReuseRejectLogic()
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
        var result = await service.RejectOrganizerApplicationAsync(organizer.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal("Rejected", result.Value.Status);
        Assert.Equal(AccountStatus.Rejected, organizer.Status);
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

        public Task<bool> ExistsByNicknameExceptUserAsync(string normalizedNickname, Guid? excludedUserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.Any(user =>
                user.NormalizedNickname == normalizedNickname
                && (excludedUserId is null || user.Id != excludedUserId)));
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

        public Task<IReadOnlyCollection<User>> GetOrganizerHistoryAsync(
            int skip,
            int take,
            string? normalizedSearch,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyCollection<User> page = ApplyOrganizerHistoryFilters(normalizedSearch)
                .OrderByDescending(user => user.ApprovedAtUtc ?? user.RejectedAtUtc ?? user.CreatedAtUtc)
                .Skip(skip)
                .Take(take)
                .ToArray();
            return Task.FromResult(page);
        }

        public Task<int> CountOrganizerHistoryAsync(
            string? normalizedSearch,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ApplyOrganizerHistoryFilters(normalizedSearch).Count());
        }

        private IEnumerable<User> ApplyOrganizerHistoryFilters(string? normalizedSearch)
        {
            IEnumerable<User> query = Users.Where(user =>
                user.Role == UserRole.Organizer
                && user.CreatedByAdminId == null
                && (user.ApprovedAtUtc != null || user.RejectedAtUtc != null));

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                query = query.Where(user =>
                    user.NormalizedEmail.Contains(normalizedSearch)
                    || (user.NormalizedOrganizerName != null && user.NormalizedOrganizerName.Contains(normalizedSearch)));
            }

            return query;
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

            return query.OrderByDescending(user => user.CreatedAtUtc);
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
