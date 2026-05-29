namespace Auth.Application.Admin.Dto;

public sealed record AdminUserResponse(
    Guid Id,
    string Email,
    string Role,
    string Status,
    string? Nickname,
    string? OrganizerName,
    string? ContactHandle,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? RejectedAtUtc,
    DateTime? DeletedAtUtc,
    Guid? CreatedByAdminId);
