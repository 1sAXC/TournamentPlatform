namespace Auth.Application.Admin.Dto;

public sealed record UpdateUserRoleRequest(
    string Role,
    string? Nickname,
    string? OrganizerName);
