namespace TournamentPlatform.Shared.Security;

public sealed record CurrentUserInfo(
    Guid UserId,
    string Role,
    string? Email,
    string? AccountStatus,
    string? Nickname,
    string? OrganizerName);
