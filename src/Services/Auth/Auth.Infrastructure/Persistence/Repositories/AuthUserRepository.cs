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

    public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.Users.CountAsync(
            user => user.Role == UserRole.Admin && user.Status == AccountStatus.Active,
            cancellationToken);
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
