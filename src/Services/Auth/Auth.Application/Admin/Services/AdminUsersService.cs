using Auth.Application.Admin.Dto;
using Auth.Application.Auth.Abstractions;
using Auth.Domain.Users;
using TournamentPlatform.Contracts.Enums;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Pagination;

namespace Auth.Application.Admin.Services;

public sealed class AdminUsersService(
    IAuthUserRepository users,
    IPasswordHashingService passwordHashingService,
    IOutboxWriter outboxWriter) : IAdminUsersService
{
    public async Task<Result<PagedResult<AdminUserResponse>>> GetUsersAsync(
        AdminUsersQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseOptionalEnum<UserRole>(query.Role, out var role))
        {
            return Result<PagedResult<AdminUserResponse>>.Failure(AdminErrors.InvalidRole);
        }

        if (!TryParseOptionalEnum<AccountStatus>(query.Status, out var status))
        {
            return Result<PagedResult<AdminUserResponse>>.Failure(AdminErrors.InvalidStatus);
        }

        var page = new PageRequest(query.PageNumber, query.PageSize);
        var normalizedSearch = NormalizeOptional(query.Search);
        var totalCount = await users.CountUsersAsync(role, status, normalizedSearch, cancellationToken);
        var items = await users.GetUsersAsync(
            (page.PageNumber - 1) * page.PageSize,
            page.PageSize,
            role,
            status,
            normalizedSearch,
            cancellationToken);

        return Result<PagedResult<AdminUserResponse>>.Success(new PagedResult<AdminUserResponse>(
            items.Select(ToResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            totalCount));
    }

    public async Task<Result<AdminUserResponse>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return user is null
            ? Result<AdminUserResponse>.Failure(AdminErrors.UserNotFound)
            : Result<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<Result<PagedResult<OrganizerApplicationResponse>>> GetOrganizerApplicationsAsync(
        OrganizerApplicationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var normalizedSearch = NormalizeOptional(query.Search);
        const UserRole role = UserRole.Organizer;
        const AccountStatus status = AccountStatus.PendingApproval;

        var totalCount = await users.CountUsersAsync(role, status, normalizedSearch, cancellationToken);
        var items = await users.GetUsersAsync(
            (page.PageNumber - 1) * page.PageSize,
            page.PageSize,
            role,
            status,
            normalizedSearch,
            cancellationToken);

        return Result<PagedResult<OrganizerApplicationResponse>>.Success(new PagedResult<OrganizerApplicationResponse>(
            items.Select(ToOrganizerApplicationResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            totalCount));
    }

    public async Task<Result<PagedResult<OrganizerApplicationResponse>>> GetOrganizerApplicationsHistoryAsync(
        OrganizerApplicationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = new PageRequest(query.PageNumber, query.PageSize);
        var normalizedSearch = NormalizeOptional(query.Search);

        var totalCount = await users.CountOrganizerHistoryAsync(normalizedSearch, cancellationToken);
        var items = await users.GetOrganizerHistoryAsync(
            (page.PageNumber - 1) * page.PageSize,
            page.PageSize,
            normalizedSearch,
            cancellationToken);

        return Result<PagedResult<OrganizerApplicationResponse>>.Success(new PagedResult<OrganizerApplicationResponse>(
            items.Select(ToOrganizerApplicationResponse).ToArray(),
            page.PageNumber,
            page.PageSize,
            totalCount));
    }

    public async Task<Result<OrganizerApplicationResponse>> GetOrganizerApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        return IsPendingOrganizer(user)
            ? Result<OrganizerApplicationResponse>.Success(ToOrganizerApplicationResponse(user!))
            : Result<OrganizerApplicationResponse>.Failure(AdminErrors.UserNotFound);
    }

    public async Task<Result<AdminUserResponse>> CreateUserAsync(
        CreateAdminUserRequest request,
        Guid adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return Result<AdminUserResponse>.Failure(AdminErrors.InvalidRole);
        }

        var normalizedEmail = NormalizeRequired(request.Email);
        if (await users.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            return Result<AdminUserResponse>.Failure(AdminErrors.DuplicateEmail);
        }

        var userResult = await CreateUserByRoleAsync(request, role, adminUserId, cancellationToken);
        if (userResult.IsFailure)
        {
            return Result<AdminUserResponse>.Failure(userResult.Error);
        }

        var user = userResult.Value;
        user.SetPasswordHash(passwordHashingService.HashPassword(user, request.Password));
        AddDomainEventsToOutbox(user);

        users.Add(user);
        await users.SaveChangesAsync(cancellationToken);

        return Result<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<Result<AdminUserResponse>> ApproveOrganizerAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<AdminUserResponse>.Failure(AdminErrors.UserNotFound);
        }

        if (user.Role != UserRole.Organizer || user.Status != AccountStatus.PendingApproval)
        {
            return Result<AdminUserResponse>.Failure(AdminErrors.OrganizerApprovalNotAllowed);
        }

        user.Approve(DateTime.UtcNow);
        AddDomainEventsToOutbox(user);
        await users.SaveChangesAsync(cancellationToken);

        return Result<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<Result<OrganizerApplicationResponse>> ApproveOrganizerApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await ApproveOrganizerAsync(userId, cancellationToken);
        return result.IsSuccess
            ? Result<OrganizerApplicationResponse>.Success(ToOrganizerApplicationResponse(result.Value))
            : Result<OrganizerApplicationResponse>.Failure(result.Error);
    }

    public async Task<Result<AdminUserResponse>> RejectOrganizerAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<AdminUserResponse>.Failure(AdminErrors.UserNotFound);
        }

        if (user.Role != UserRole.Organizer || user.Status != AccountStatus.PendingApproval)
        {
            return Result<AdminUserResponse>.Failure(AdminErrors.OrganizerRejectNotAllowed);
        }

        user.Reject(DateTime.UtcNow);
        AddDomainEventsToOutbox(user);
        await users.SaveChangesAsync(cancellationToken);

        return Result<AdminUserResponse>.Success(ToResponse(user));
    }

    public async Task<Result<OrganizerApplicationResponse>> RejectOrganizerApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var result = await RejectOrganizerAsync(userId, cancellationToken);
        return result.IsSuccess
            ? Result<OrganizerApplicationResponse>.Success(ToOrganizerApplicationResponse(result.Value))
            : Result<OrganizerApplicationResponse>.Failure(result.Error);
    }

    public async Task<Result> DeleteUserAsync(
        Guid userId,
        Guid currentAdminUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(AdminErrors.UserNotFound);
        }

        if (user.Id == currentAdminUserId
            && user.Role == UserRole.Admin
            && user.Status == AccountStatus.Active
            && await users.CountActiveAdminsAsync(cancellationToken) <= 1)
        {
            return Result.Failure(AdminErrors.LastAdminDeleteNotAllowed);
        }

        user.SoftDelete(DateTime.UtcNow);
        AddDomainEventsToOutbox(user);
        await users.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<ResetPasswordResponse>> ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<ResetPasswordResponse>.Failure(AdminErrors.UserNotFound);
        }

        var generated = string.IsNullOrWhiteSpace(request.TemporaryPassword);
        var temporaryPassword = generated ? GenerateTemporaryPassword() : request.TemporaryPassword!.Trim();
        user.SetPasswordHash(passwordHashingService.HashPassword(user, temporaryPassword));

        await users.SaveChangesAsync(cancellationToken);

        return Result<ResetPasswordResponse>.Success(new ResetPasswordResponse(
            user.Id,
            generated ? temporaryPassword : null));
    }

    public async Task<Result<AdminUserResponse>> UpdateRoleAsync(
        Guid userId,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result<AdminUserResponse>.Failure(AdminErrors.UserNotFound);
        }

        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            return Result<AdminUserResponse>.Failure(AdminErrors.InvalidRole);
        }

        var validationResult = await ValidateRoleSpecificFieldsAsync(user.Id, role, request.Nickname, request.OrganizerName, cancellationToken);
        if (validationResult.IsFailure)
        {
            return Result<AdminUserResponse>.Failure(validationResult.Error);
        }

        user.ChangeRole(role, request.Nickname?.Trim(), request.OrganizerName?.Trim(), DateTime.UtcNow);
        AddDomainEventsToOutbox(user);
        await users.SaveChangesAsync(cancellationToken);

        return Result<AdminUserResponse>.Success(ToResponse(user));
    }

    private async Task<Result<User>> CreateUserByRoleAsync(
        CreateAdminUserRequest request,
        UserRole role,
        Guid adminUserId,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        if (role == UserRole.Player)
        {
            var validation = await ValidateRoleSpecificFieldsAsync(null, role, request.Nickname, request.OrganizerName, cancellationToken);
            if (validation.IsFailure)
            {
                return Result<User>.Failure(validation.Error);
            }

            return Result<User>.Success(User.CreatePlayer(
                request.Email.Trim(),
                passwordHash: "temporary",
                request.Nickname!.Trim(),
                now));
        }

        if (role == UserRole.Organizer)
        {
            var validation = await ValidateRoleSpecificFieldsAsync(null, role, request.Nickname, request.OrganizerName, cancellationToken);
            if (validation.IsFailure)
            {
                return Result<User>.Failure(validation.Error);
            }

            return Result<User>.Success(User.CreateOrganizerByAdmin(
                request.Email.Trim(),
                passwordHash: "temporary",
                request.OrganizerName!.Trim(),
                adminUserId,
                now));
        }

        return Result<User>.Success(User.CreateAdminByAdmin(
            request.Email.Trim(),
            passwordHash: "temporary",
            adminUserId,
            now));
    }

    private async Task<Result> ValidateRoleSpecificFieldsAsync(
        Guid? userId,
        UserRole role,
        string? nickname,
        string? organizerName,
        CancellationToken cancellationToken)
    {
        if (role == UserRole.Player)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                return Result.Failure(AdminErrors.PlayerNicknameRequired);
            }

            var normalizedNickname = NormalizeRequired(nickname);
            if (await users.ExistsByNicknameExceptUserAsync(normalizedNickname, userId, cancellationToken))
            {
                return Result.Failure(AdminErrors.DuplicateNickname);
            }
        }

        if (role == UserRole.Organizer && string.IsNullOrWhiteSpace(organizerName))
        {
            return Result.Failure(AdminErrors.OrganizerNameRequired);
        }

        return Result.Success();
    }

    private void AddDomainEventsToOutbox(User user)
    {
        foreach (var domainEvent in user.DomainEvents)
        {
            outboxWriter.Add(domainEvent);
        }

        user.ClearDomainEvents();
    }

    private static AdminUserResponse ToResponse(User user)
    {
        return new AdminUserResponse(
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.Status.ToString(),
            user.Nickname,
            user.OrganizerName,
            user.CreatedAtUtc,
            user.ApprovedAtUtc,
            user.RejectedAtUtc,
            user.DeletedAtUtc,
            user.CreatedByAdminId);
    }

    private static OrganizerApplicationResponse ToOrganizerApplicationResponse(User user)
    {
        return new OrganizerApplicationResponse(
            user.Id,
            user.Email,
            user.Status.ToString(),
            user.OrganizerName ?? string.Empty,
            user.CreatedAtUtc,
            user.ApprovedAtUtc,
            user.RejectedAtUtc);
    }

    private static OrganizerApplicationResponse ToOrganizerApplicationResponse(AdminUserResponse user)
    {
        return new OrganizerApplicationResponse(
            user.Id,
            user.Email,
            user.Status,
            user.OrganizerName ?? string.Empty,
            user.CreatedAtUtc,
            user.ApprovedAtUtc,
            user.RejectedAtUtc);
    }

    private static bool IsPendingOrganizer(User? user)
    {
        return user is { Role: UserRole.Organizer, Status: AccountStatus.PendingApproval };
    }

    private static bool TryParseOptionalEnum<TEnum>(string? value, out TEnum? result)
        where TEnum : struct
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            return true;
        }

        if (Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
        {
            result = parsed;
            return true;
        }

        result = null;
        return false;
    }

    private static string NormalizeRequired(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be empty.", nameof(value));
        }

        return value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static string GenerateTemporaryPassword()
    {
        return $"Tmp-{Guid.NewGuid():N}1a";
    }
}
