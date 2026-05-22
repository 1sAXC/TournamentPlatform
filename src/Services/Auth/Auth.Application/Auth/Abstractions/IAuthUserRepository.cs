using Auth.Domain.Users;
using TournamentPlatform.Contracts.Enums;

namespace Auth.Application.Auth.Abstractions;

public interface IAuthUserRepository
{
    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNicknameAsync(string normalizedNickname, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNicknameExceptUserAsync(string normalizedNickname, Guid? excludedUserId, CancellationToken cancellationToken = default);
    Task<User?> GetByLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<User>> GetUsersAsync(
        int skip,
        int take,
        UserRole? role,
        AccountStatus? status,
        string? normalizedSearch,
        CancellationToken cancellationToken = default);
    Task<int> CountUsersAsync(
        UserRole? role,
        AccountStatus? status,
        string? normalizedSearch,
        CancellationToken cancellationToken = default);
    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default);
    void Add(User user);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
