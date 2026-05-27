using Auth.Application.Auth.Abstractions;
using Auth.Application.Auth.Dto;
using Auth.Domain.Users;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Shared.Common;

namespace Auth.Application.Auth.Services;

public sealed class AuthService(
    IAuthUserRepository users,
    IPasswordHashingService passwordHashingService,
    IJwtTokenGenerator jwtTokenGenerator,
    IOutboxWriter outboxWriter) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterPlayerAsync(
        RegisterPlayerRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeRequired(request.Email);
        if (await users.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return Result<AuthResponse>.Failure(AuthErrors.DuplicateEmail);
        }

        var normalizedNickname = NormalizeRequired(request.Nickname);
        if (await users.ExistsByNicknameAsync(normalizedNickname, cancellationToken))
        {
            return Result<AuthResponse>.Failure(AuthErrors.DuplicateNickname);
        }

        var user = User.CreatePlayer(
            request.Email.Trim(),
            passwordHash: "temporary",
            request.Nickname.Trim(),
            DateTime.UtcNow);

        user.SetPasswordHash(passwordHashingService.HashPassword(user, request.Password));
        AddDomainEventsToOutbox(user);

        users.Add(user);
        await users.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(CreateAuthResponse(user));
    }

    public async Task<Result<AuthResponse>> RegisterOrganizerAsync(
        RegisterOrganizerRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = NormalizeRequired(request.Email);
        var existing = await users.GetByLoginAsync(normalizedEmail, cancellationToken);
        if (existing is not null && existing.NormalizedEmail == normalizedEmail)
        {
            // A previously rejected organizer may re-apply with the same email:
            // reuse the existing record (the email column is unique) and reset
            // it to a fresh pending application.
            if (existing.Role == UserRole.Organizer && existing.Status == AccountStatus.Rejected)
            {
                existing.ResubmitOrganizerApplication(
                    request.OrganizerName.Trim(),
                    passwordHashingService.HashPassword(existing, request.Password),
                    DateTime.UtcNow);
                AddDomainEventsToOutbox(existing);
                await users.SaveChangesAsync(cancellationToken);

                return Result<AuthResponse>.Success(CreateAuthResponse(existing));
            }

            return Result<AuthResponse>.Failure(AuthErrors.DuplicateEmail);
        }

        var user = User.CreateOrganizerSelfRegistration(
            request.Email.Trim(),
            passwordHash: "temporary",
            request.OrganizerName.Trim(),
            DateTime.UtcNow);

        user.SetPasswordHash(passwordHashingService.HashPassword(user, request.Password));
        AddDomainEventsToOutbox(user);

        users.Add(user);
        await users.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(CreateAuthResponse(user));
    }

    public async Task<Result<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedLogin = NormalizeRequired(request.Login);
        var user = await users.GetByLoginAsync(normalizedLogin, cancellationToken);

        if (user is null || !passwordHashingService.VerifyPassword(user, request.Password))
        {
            return Result<AuthResponse>.Failure(AuthErrors.InvalidCredentials);
        }

        if (user.Status is AccountStatus.Deleted or AccountStatus.Rejected)
        {
            return Result<AuthResponse>.Failure(AuthErrors.AccessDenied);
        }

        return Result<AuthResponse>.Success(CreateAuthResponse(user));
    }

    public async Task<Result<CurrentUserResponse>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            return Result<CurrentUserResponse>.Failure(AuthErrors.UserNotFound);
        }

        // Mirror login-time gate: deleted/rejected accounts cannot use any
        // authenticated endpoint. Returning AccessDenied makes /me reply 401,
        // which the SPA picks up via its interceptor to auto-logout.
        if (user.Status is AccountStatus.Deleted or AccountStatus.Rejected)
        {
            return Result<CurrentUserResponse>.Failure(AuthErrors.AccessDenied);
        }

        return Result<CurrentUserResponse>.Success(CreateCurrentUserResponse(user));
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AuthErrors.UserNotFound);
        }

        if (!passwordHashingService.VerifyPassword(user, request.CurrentPassword))
        {
            return Result.Failure(AuthErrors.InvalidCurrentPassword);
        }

        user.SetPasswordHash(passwordHashingService.HashPassword(user, request.NewPassword));
        await users.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = jwtTokenGenerator.Generate(user);
        return new AuthResponse(token.AccessToken, token.ExpiresAtUtc, CreateCurrentUserResponse(user));
    }

    private static CurrentUserResponse CreateCurrentUserResponse(User user)
    {
        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.Status.ToString(),
            user.Nickname,
            user.OrganizerName);
    }

    private void AddDomainEventsToOutbox(User user)
    {
        foreach (var domainEvent in user.DomainEvents)
        {
            outboxWriter.Add(domainEvent);
        }

        user.ClearDomainEvents();
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", nameof(value));
        }

        return value.Trim().ToUpperInvariant();
    }
}
