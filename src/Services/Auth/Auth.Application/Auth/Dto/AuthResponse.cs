namespace Auth.Application.Auth.Dto;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    CurrentUserResponse User);
