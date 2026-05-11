namespace Auth.Application.Auth.Dto;

public sealed record RegisterOrganizerRequest(string OrganizerName, string Email, string Password);
