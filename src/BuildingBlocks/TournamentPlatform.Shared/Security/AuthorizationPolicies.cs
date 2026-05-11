using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TournamentPlatform.Shared.Security;

public static class AuthorizationPolicies
{
    public const string RequirePlayer = nameof(RequirePlayer);
    public const string RequireOrganizer = nameof(RequireOrganizer);
    public const string RequireActiveOrganizer = nameof(RequireActiveOrganizer);
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireOrganizerOrAdmin = nameof(RequireOrganizerOrAdmin);

    public static void AddTournamentPlatformPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(RequirePlayer, policy => policy.RequireRole("Player"));
        options.AddPolicy(RequireOrganizer, policy => policy.RequireRole("Organizer"));
        options.AddPolicy(RequireActiveOrganizer, policy => policy
            .RequireRole("Organizer")
            .RequireClaim("account_status", "Active"));
        options.AddPolicy(RequireAdmin, policy => policy.RequireRole("Admin"));
        options.AddPolicy(RequireOrganizerOrAdmin, policy => policy.RequireAssertion(context =>
            context.User.IsInRole("Admin")
            || (context.User.IsInRole("Organizer")
                && string.Equals(
                    context.User.FindFirst("account_status")?.Value,
                    "Active",
                    StringComparison.OrdinalIgnoreCase))));

        options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    }
}
