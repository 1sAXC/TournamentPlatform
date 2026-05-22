namespace Auth.Application.Admin.Dto;

public sealed record OrganizerApplicationsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Search = null);
