namespace Auth.Application.Auth.Abstractions;

public sealed record JwtToken(string AccessToken, DateTime ExpiresAtUtc);
