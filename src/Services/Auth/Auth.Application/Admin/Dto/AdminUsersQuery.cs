namespace Auth.Application.Admin.Dto;

public sealed record AdminUsersQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Role = null,
    string? Status = null,
    string? Search = null);
