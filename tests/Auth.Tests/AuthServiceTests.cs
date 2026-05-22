using Auth.Application.Auth;
using Auth.Application.Auth.Abstractions;
using Auth.Application.Auth.Dto;
using Auth.Application.Auth.Services;
using Auth.Domain.Users;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Auth.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterPlayer_ShouldCreateActiveUserHashPasswordAndWriteUserCreatedToOutbox()
    {
        var repository = new InMemoryAuthUserRepository();
        var outbox = new InMemoryOutboxWriter();
        var service = CreateService(repository, outbox);

        var result = await service.RegisterPlayerAsync(new RegisterPlayerRequest(
            "PlayerOne",
            "player@example.com",
            "Password1"));

        Assert.True(result.IsSuccess);
        var user = Assert.Single(repository.Users);
        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.NotEqual("Password1", user.PasswordHash);
        Assert.Single(outbox.Events);
        Assert.IsType<UserCreatedEvent>(outbox.Events[0]);
    }

    [Fact]
    public async Task RegisterOrganizer_ShouldCreatePendingUserWithoutOutboxEvent()
    {
        var repository = new InMemoryAuthUserRepository();
        var outbox = new InMemoryOutboxWriter();
        var service = CreateService(repository, outbox);

        var result = await service.RegisterOrganizerAsync(new RegisterOrganizerRequest(
            "Organizer Inc",
            "organizer@example.com",
            "Password1"));

        Assert.True(result.IsSuccess);
        var user = Assert.Single(repository.Users);
        Assert.Equal(AccountStatus.PendingApproval, user.Status);
        Assert.Empty(outbox.Events);
        Assert.Equal("PendingApproval", result.Value.User.AccountStatus);
    }

    [Fact]
    public async Task RegisterPlayer_ShouldRejectDuplicateEmail()
    {
        var repository = new InMemoryAuthUserRepository();
        var service = CreateService(repository, new InMemoryOutboxWriter());

        await service.RegisterPlayerAsync(new RegisterPlayerRequest("PlayerOne", "player@example.com", "Password1"));
        var duplicate = await service.RegisterPlayerAsync(new RegisterPlayerRequest("PlayerTwo", "PLAYER@example.com", "Password1"));

        Assert.True(duplicate.IsFailure);
        Assert.Equal(AuthErrors.DuplicateEmail, duplicate.Error);
    }

    [Fact]
    public async Task RegisterPlayer_ShouldRejectDuplicateNickname()
    {
        var repository = new InMemoryAuthUserRepository();
        var service = CreateService(repository, new InMemoryOutboxWriter());

        await service.RegisterPlayerAsync(new RegisterPlayerRequest("PlayerOne", "player1@example.com", "Password1"));
        var duplicate = await service.RegisterPlayerAsync(new RegisterPlayerRequest("playerone", "player2@example.com", "Password1"));

        Assert.True(duplicate.IsFailure);
        Assert.Equal(AuthErrors.DuplicateNickname, duplicate.Error);
    }

    [Fact]
    public async Task Login_ShouldAcceptPlayerNickname()
    {
        var repository = new InMemoryAuthUserRepository();
        var service = CreateService(repository, new InMemoryOutboxWriter());
        await service.RegisterPlayerAsync(new RegisterPlayerRequest("PlayerOne", "player@example.com", "Password1"));

        var result = await service.LoginAsync(new LoginRequest("playerone", "Password1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Player", result.Value.User.Role);
        Assert.Equal("Active", result.Value.User.AccountStatus);
    }

    [Fact]
    public async Task Login_ShouldAllowPendingOrganizerAndReturnPendingApprovalStatus()
    {
        var repository = new InMemoryAuthUserRepository();
        var service = CreateService(repository, new InMemoryOutboxWriter());
        await service.RegisterOrganizerAsync(new RegisterOrganizerRequest("Organizer Inc", "organizer@example.com", "Password1"));

        var result = await service.LoginAsync(new LoginRequest("organizer@example.com", "Password1"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Organizer", result.Value.User.Role);
        Assert.Equal("PendingApproval", result.Value.User.AccountStatus);
    }

    [Fact]
    public async Task ChangePassword_ShouldAllowPlayerAndReplaceOldPassword()
    {
        var repository = new InMemoryAuthUserRepository();
        var service = CreateService(repository, new InMemoryOutboxWriter());
        await service.RegisterPlayerAsync(new RegisterPlayerRequest("PlayerOne", "player@example.com", "Password1"));
        var player = Assert.Single(repository.Users);

        var result = await service.ChangePasswordAsync(player.Id, new ChangePasswordRequest("Password1", "NewPassword1"));
        var oldLogin = await service.LoginAsync(new LoginRequest("player@example.com", "Password1"));
        var newLogin = await service.LoginAsync(new LoginRequest("player@example.com", "NewPassword1"));

        Assert.True(result.IsSuccess);
        Assert.True(oldLogin.IsFailure);
        Assert.Equal(AuthErrors.InvalidCredentials, oldLogin.Error);
        Assert.True(newLogin.IsSuccess);
        Assert.Equal(player.Id, newLogin.Value.User.Id);
    }

    [Fact]
    public async Task ChangePassword_ShouldAllowOrganizer()
    {
        var repository = new InMemoryAuthUserRepository();
        var service = CreateService(repository, new InMemoryOutboxWriter());
        await service.RegisterOrganizerAsync(new RegisterOrganizerRequest("Organizer Inc", "organizer@example.com", "Password1"));
        var organizer = Assert.Single(repository.Users);

        var result = await service.ChangePasswordAsync(organizer.Id, new ChangePasswordRequest("Password1", "NewPassword1"));
        var login = await service.LoginAsync(new LoginRequest("organizer@example.com", "NewPassword1"));

        Assert.True(result.IsSuccess);
        Assert.True(login.IsSuccess);
        Assert.Equal("Organizer", login.Value.User.Role);
    }

    [Fact]
    public async Task ChangePassword_ShouldAllowAdmin()
    {
        var repository = new InMemoryAuthUserRepository();
        var admin = User.CreateAdmin(Guid.NewGuid(), "admin@example.com", "temporary", DateTime.UtcNow);
        admin.SetPasswordHash("HASHED:Password1");
        repository.Users.Add(admin);
        var service = CreateService(repository, new InMemoryOutboxWriter());

        var result = await service.ChangePasswordAsync(admin.Id, new ChangePasswordRequest("Password1", "NewPassword1"));
        var login = await service.LoginAsync(new LoginRequest("admin@example.com", "NewPassword1"));

        Assert.True(result.IsSuccess);
        Assert.True(login.IsSuccess);
        Assert.Equal("Admin", login.Value.User.Role);
    }

    [Fact]
    public async Task ChangePassword_ShouldRejectInvalidCurrentPassword()
    {
        var repository = new InMemoryAuthUserRepository();
        var service = CreateService(repository, new InMemoryOutboxWriter());
        await service.RegisterPlayerAsync(new RegisterPlayerRequest("PlayerOne", "player@example.com", "Password1"));
        var player = Assert.Single(repository.Users);

        var result = await service.ChangePasswordAsync(player.Id, new ChangePasswordRequest("WrongPassword1", "NewPassword1"));
        var oldLogin = await service.LoginAsync(new LoginRequest("player@example.com", "Password1"));
        var newLogin = await service.LoginAsync(new LoginRequest("player@example.com", "NewPassword1"));

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidCurrentPassword, result.Error);
        Assert.True(oldLogin.IsSuccess);
        Assert.True(newLogin.IsFailure);
    }

    private static AuthService CreateService(
        InMemoryAuthUserRepository repository,
        InMemoryOutboxWriter outbox)
    {
        return new AuthService(
            repository,
            new StubPasswordHashingService(),
            new StubJwtTokenGenerator(),
            outbox);
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

    private sealed class StubJwtTokenGenerator : IJwtTokenGenerator
    {
        public JwtToken Generate(User user)
        {
            return new JwtToken("access-token", DateTime.UtcNow.AddMinutes(60));
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
}
