using TournamentPlatform.Shared.Common;

namespace Auth.Application.Auth;

public static class AuthErrors
{
    public static readonly Error DuplicateEmail = new("Auth.DuplicateEmail", "User with this email already exists.");
    public static readonly Error DuplicateNickname = new("Auth.DuplicateNickname", "Player with this nickname already exists.");
    public static readonly Error InvalidCredentials = new("Auth.InvalidCredentials", "Invalid login or password.");
    public static readonly Error InvalidCurrentPassword = new("Auth.InvalidCurrentPassword", "Current password is invalid.");
    public static readonly Error AccessDenied = new("Auth.AccessDenied", "Account is not allowed to sign in.");
    public static readonly Error UserNotFound = new("Auth.UserNotFound", "User was not found.");
}
