using Auth.Application.Auth.Dto;
using TournamentPlatform.Shared.Common;

namespace Auth.Application.Auth.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterPlayerAsync(RegisterPlayerRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RegisterOrganizerAsync(RegisterOrganizerRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<CurrentUserResponse>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<CurrentUserResponse>> UpdateContactHandleAsync(Guid userId, UpdateContactHandleRequest request, CancellationToken cancellationToken = default);
}
