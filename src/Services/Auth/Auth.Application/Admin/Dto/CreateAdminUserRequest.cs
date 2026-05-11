namespace Auth.Application.Admin.Dto;

public sealed record CreateAdminUserRequest(
    string Role,
    string Email,
    string Password,
    string? Nickname,
    string? OrganizerName);
