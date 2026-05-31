namespace Auth.Application.Auth.Dto;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    string Role,
    string AccountStatus,
    string? Nickname,
    string? OrganizerName,
    string? ContactHandle);
