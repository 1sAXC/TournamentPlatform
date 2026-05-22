using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TournamentPlatform.Shared.Security;

namespace Integration.Tests;

public sealed class SecurityPolicyTests
{
    [Fact]
    public async Task RequireActiveOrganizer_AllowsOnlyActiveOrganizer()
    {
        var authorizationService = CreateAuthorizationService();

        var activeOrganizer = await authorizationService.AuthorizeAsync(
            User("Organizer", "Active"),
            resource: null,
            AuthorizationPolicies.RequireActiveOrganizer);
        var pendingOrganizer = await authorizationService.AuthorizeAsync(
            User("Organizer", "PendingApproval"),
            resource: null,
            AuthorizationPolicies.RequireActiveOrganizer);
        var player = await authorizationService.AuthorizeAsync(
            User("Player", "Active"),
            resource: null,
            AuthorizationPolicies.RequireActiveOrganizer);

        Assert.True(activeOrganizer.Succeeded);
        Assert.False(pendingOrganizer.Succeeded);
        Assert.False(player.Succeeded);
    }

    [Fact]
    public async Task RequireOrganizerOrAdmin_AllowsAdminAndActiveOrganizerOnly()
    {
        var authorizationService = CreateAuthorizationService();

        var admin = await authorizationService.AuthorizeAsync(
            User("Admin", "Active"),
            resource: null,
            AuthorizationPolicies.RequireOrganizerOrAdmin);
        var activeOrganizer = await authorizationService.AuthorizeAsync(
            User("Organizer", "Active"),
            resource: null,
            AuthorizationPolicies.RequireOrganizerOrAdmin);
        var pendingOrganizer = await authorizationService.AuthorizeAsync(
            User("Organizer", "PendingApproval"),
            resource: null,
            AuthorizationPolicies.RequireOrganizerOrAdmin);
        var player = await authorizationService.AuthorizeAsync(
            User("Player", "Active"),
            resource: null,
            AuthorizationPolicies.RequireOrganizerOrAdmin);

        Assert.True(admin.Succeeded);
        Assert.True(activeOrganizer.Succeeded);
        Assert.False(pendingOrganizer.Succeeded);
        Assert.False(player.Succeeded);
    }

    [Fact]
    public async Task RequireAdmin_AllowsOnlyAdmin()
    {
        var authorizationService = CreateAuthorizationService();

        var admin = await authorizationService.AuthorizeAsync(
            User("Admin", "Active"),
            resource: null,
            AuthorizationPolicies.RequireAdmin);
        var organizer = await authorizationService.AuthorizeAsync(
            User("Organizer", "Active"),
            resource: null,
            AuthorizationPolicies.RequireAdmin);
        var player = await authorizationService.AuthorizeAsync(
            User("Player", "Active"),
            resource: null,
            AuthorizationPolicies.RequireAdmin);

        Assert.True(admin.Succeeded);
        Assert.False(organizer.Succeeded);
        Assert.False(player.Succeeded);
    }

    [Fact]
    public void CurrentUser_IsReadFromJwtClaims()
    {
        var userId = Guid.NewGuid();
        var principal = User(
            "Player",
            "Active",
            userId,
            "player@example.test",
            "PlayerOne",
            organizerName: null);

        var parsed = principal.TryGetCurrentUser(out var currentUser);

        Assert.True(parsed);
        Assert.Equal(userId, currentUser.UserId);
        Assert.Equal("Player", currentUser.Role);
        Assert.Equal("player@example.test", currentUser.Email);
        Assert.Equal("Active", currentUser.AccountStatus);
        Assert.Equal("PlayerOne", currentUser.Nickname);
    }

    private static IAuthorizationService CreateAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options => options.AddTournamentPlatformPolicies());
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal User(
        string role,
        string accountStatus,
        Guid? userId = null,
        string? email = null,
        string? nickname = null,
        string? organizerName = "Organizer")
    {
        var claims = new List<Claim>
        {
            new("sub", (userId ?? Guid.NewGuid()).ToString()),
            new(ClaimTypes.Role, role),
            new("account_status", accountStatus)
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        if (!string.IsNullOrWhiteSpace(nickname))
        {
            claims.Add(new Claim("nickname", nickname));
        }

        if (!string.IsNullOrWhiteSpace(organizerName))
        {
            claims.Add(new Claim("organizer_name", organizerName));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
