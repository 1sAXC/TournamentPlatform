namespace Auth.Application.Auth.Dto;

public sealed record UserLookupItem(
    Guid Id,
    string? Nickname,
    string? OrganizerName,
    string? ContactHandle,
    string Email);
