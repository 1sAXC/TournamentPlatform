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
            DateTime.UtcNow);

        Assert.Equal(UserRole.Organizer, user.Role);
        Assert.Equal(AccountStatus.PendingApproval, user.Status);
        Assert.Equal("ORGANIZER@EXAMPLE.COM", user.NormalizedEmail);
        Assert.Equal("ORGANIZER INC", user.NormalizedOrganizerName);
        Assert.DoesNotContain(user.DomainEvents, domainEvent => domainEvent is UserCreatedEvent);
    }

    [Fact]
    public void SoftDelete_ShouldMarkAccountDeletedAndSetDeletedAt()
    {
        var user = User.CreatePlayer(
            "player@example.com",
            "hashed-password",
            "PlayerOne",
            DateTime.UtcNow);

        var deletedAtUtc = DateTime.UtcNow;
        user.SoftDelete(deletedAtUtc);

        Assert.Equal(AccountStatus.Deleted, user.Status);
        Assert.Equal(deletedAtUtc, user.DeletedAtUtc);
    }
}
