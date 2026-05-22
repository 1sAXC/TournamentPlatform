namespace Auth.Application.Auth.Dto;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
