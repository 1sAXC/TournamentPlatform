using Auth.Application.Auth.Abstractions;
using Auth.Domain.Users;
using Microsoft.EntityFrameworkCore;
using TournamentPlatform.Contracts.Enums;

namespace Auth.Infrastructure.Persistence.Repositories;

public sealed class AuthUserRepository(AuthDbContext dbContext) : IAuthUserRepository
{
    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByNicknameAsync(string normalizedNickname, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(user => user.NormalizedNickname == normalizedNickname, cancellationToken);
    }

    public Task<bool> ExistsByNicknameExceptUserAsync(
        string normalizedNickname,
        Guid? excludedUserId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users.AnyAsync(
            user => user.NormalizedNickname == normalizedNickname
                && (!excludedUserId.HasValue || user.Id != excludedUserId.Value),
            cancellationToken);
    }

    public Task<User?> GetByLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.FirstOrDefaultAsync(
            user => user.NormalizedEmail == normalizedLogin || user.NormalizedNickname == normalizedLogin,
            cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Users.FirstOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetUsersAsync(
        int skip,
        int take,
        UserRole? role,
        AccountStatus? status,
        string? normalizedSearch,
        CancellationToken cancellationToken = default)
    {
        return await ApplyFilters(role, status, normalizedSearch)
            .OrderByDescending(user => user.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountUsersAsync(
        UserRole? role,
        AccountStatus? status,
        string? normalizedSearch,
        CancellationToken cancellationToken = default)
    {
        return ApplyFilters(role, status, normalizedSearch).CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetOrganizerHistoryAsync(
        int skip,
        int take,
        string? normalizedSearch,
        CancellationToken cancellationToken = default)
    {
        return await ApplyOrganizerHistoryFilters(normalizedSearch)
            .OrderByDescending(user => user.ApprovedAtUtc ?? user.RejectedAtUtc ?? user.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> CountOrganizerHistoryAsync(
        string? normalizedSearch,
        CancellationToken cancellationToken = default)
    {
        return ApplyOrganizerHistoryFilters(normalizedSearch).CountAsync(cancellationToken);
    }

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Users.CountAsync(
            user => user.Role == UserRole.Admin && user.Status == AccountStatus.Active,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<User>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        // EF Core translates Contains() over a Guid array into a parameterised
        // IN clause, which is fine for the few-dozen-id lookups we expect from
        // the Tournament service (match team rosters).
        return await dbContext.Users
            .Where(user => ids.Contains(user.Id))
            .ToArrayAsync(cancellationToken);
    }

    // Organizer "history" = self-registered organizers whose application has
    // been decided on — either approved (ApprovedAtUtc set) or rejected
    // (RejectedAtUtc set). Excludes pending applications (still in the inbox)
    // and organizers admin created directly (CreatedByAdminId set — no
    // application ever existed).
    private IQueryable<User> ApplyOrganizerHistoryFilters(string? normalizedSearch)
    {
        var query = dbContext.Users
            .Where(user => user.Role == UserRole.Organizer
                && user.CreatedByAdminId == null
                && (user.ApprovedAtUtc != null || user.RejectedAtUtc != null));

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(user =>
                user.NormalizedEmail.Contains(normalizedSearch)
                || (user.NormalizedOrganizerName != null && user.NormalizedOrganizerName.Contains(normalizedSearch)));
        }

        return query;
    }

    public void Add(User user)
    {
        dbContext.Users.Add(user);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<User> ApplyFilters(UserRole? role, AccountStatus? status, string? normalizedSearch)
    {
        var query = dbContext.Users.AsQueryable();

        if (role.HasValue)
        {
            query = query.Where(user => user.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(user => user.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            query = query.Where(user =>
                user.NormalizedEmail.Contains(normalizedSearch)
                || (user.NormalizedNickname != null && user.NormalizedNickname.Contains(normalizedSearch))
                || (user.NormalizedOrganizerName != null && user.NormalizedOrganizerName.Contains(normalizedSearch)));
        }

        return query;
    }
}
