namespace Auth.Application.Admin.Dto;

public sealed record OrganizerApplicationResponse(
    Guid Id,
    string Email,
    string Status,
    string OrganizerName,
    string? ContactHandle,
    DateTime CreatedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? RejectedAtUtc);
