namespace Auth.Application.Admin.Dto;

public sealed record ResetPasswordResponse(Guid UserId, string? TemporaryPassword);
