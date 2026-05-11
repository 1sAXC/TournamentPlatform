using System.Security.Claims;

namespace TournamentPlatform.Shared.Security;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetCurrentUser(this ClaimsPrincipal principal, out CurrentUserInfo currentUser)
    {
        currentUser = default!;
        var userIdClaim = principal.FindFirstValue("sub")
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return false;
        }

        currentUser = new CurrentUserInfo(
            userId,
            principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
            principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email"),
            principal.FindFirstValue("account_status"),
            principal.FindFirstValue("nickname"),
            principal.FindFirstValue("organizer_name"));

        return true;
    }
}
