namespace Auth.Application.Auth.Dto;

public sealed record RegisterPlayerRequest(string Nickname, string Email, string Password);
