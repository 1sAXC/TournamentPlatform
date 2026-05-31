using Auth.Domain.Users;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Contracts.Events;

namespace Auth.Tests;

public sealed class UserTests
{
    [Fact]
    public void CreatePlayer_ShouldCreateActiveAccountAndUserCreatedEvent()
    {
        var user = User.CreatePlayer(
            "player@example.com",
            "hashed-password",
            "PlayerOne",
            "@player_one",
            DateTime.UtcNow);

        Assert.Equal(UserRole.Player, user.Role);
        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Equal("PLAYER@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal("PLAYERONE", user.NormalizedNickname);
        Assert.Contains(user.DomainEvents, domainEvent => domainEvent is UserCreatedEvent);
    }

    [Fact]
    public void CreateOrganizerSelfRegistration_ShouldCreatePendingAccountWithoutUserCreatedEvent()
    {
        var user = User.CreateOrganizerSelfRegistration(
            "organizer@example.com",
            "hashed-password",
            "Organizer Inc",
            "@org_inc",
            DateTime.UtcNow);

        Assert.Equal(UserRole.Organizer, user.Role);
        Assert.Equal(AccountStatus.PendingApproval, user.Status);
        Assert.Equal("ORGANIZER@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal("ORGANIZER INC", user.NormalizedOrganizerName);
        Assert.DoesNotContain(user.DomainEvents, domainEvent => domainEvent is UserCreatedEvent);
    }

    [Fact]
    public void Block_ShouldMarkAccountBlockedAndSetBlockedAt()
    {
        var user = User.CreatePlayer(
            "player@example.com",
            "hashed-password",
            "PlayerOne",
            "@player_one",
            DateTime.UtcNow);

        var blockedAtUtc = DateTime.UtcNow;
        user.Block(blockedAtUtc);

        Assert.Equal(AccountStatus.Blocked, user.Status);
        Assert.Equal(blockedAtUtc, user.BlockedAtUtc);
        Assert.Contains(user.DomainEvents, domainEvent => domainEvent is UserBlockedEvent);
    }

    [Fact]
    public void Unblock_FromBlocked_RestoresActiveAndEmitsUserCreated()
    {
        var user = User.CreatePlayer(
            "player@example.com",
            "hashed-password",
            "PlayerOne",
            "@player_one",
            DateTime.UtcNow);
        user.Block(DateTime.UtcNow);
        user.ClearDomainEvents();

        user.Unblock(DateTime.UtcNow);

        Assert.Equal(AccountStatus.Active, user.Status);
        Assert.Null(user.BlockedAtUtc);
        var created = Assert.Single(user.DomainEvents.OfType<UserCreatedEvent>());
        Assert.Equal("Unblock", created.CreationSource);
    }

    [Fact]
    public void Unblock_FromActive_Throws()
    {
        var user = User.CreatePlayer(
            "player@example.com",
            "hashed-password",
            "PlayerOne",
            "@player_one",
            DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => user.Unblock(DateTime.UtcNow));
    }

    [Fact]
    public void CreatePlayer_ShouldStoreTrimmedContactHandle()
    {
        var user = User.CreatePlayer(
            "player@example.com",
            "hashed-password",
            "PlayerOne",
            "  @player_one  ",
            DateTime.UtcNow);

        Assert.Equal("@player_one", user.ContactHandle);
    }

    [Fact]
    public void CreatePlayer_ShouldRejectEmptyContactHandle()
    {
        Assert.Throws<ArgumentException>(() => User.CreatePlayer(
            "player@example.com",
            "hashed-password",
            "PlayerOne",
            "   ",
            DateTime.UtcNow));
    }

    [Fact]
    public void CreateAdmin_ShouldHaveNullContactHandle()
    {
        var admin = User.CreateAdmin(Guid.NewGuid(), "admin@example.com", "hash", DateTime.UtcNow);

        Assert.Null(admin.ContactHandle);
    }

    [Fact]
    public void UpdateContactHandle_ShouldChangeValueForPlayer()
    {
        var user = User.CreatePlayer(
            "player@example.com",
            "hashed-password",
            "PlayerOne",
            "@old",
            DateTime.UtcNow);

        user.UpdateContactHandle("@new_handle");

        Assert.Equal("@new_handle", user.ContactHandle);
    }

    [Fact]
    public void UpdateContactHandle_ShouldThrowForAdmin()
    {
        var admin = User.CreateAdmin(Guid.NewGuid(), "admin@example.com", "hash", DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() => admin.UpdateContactHandle("@whatever"));
    }
}
