using Auth.Application.Admin.Dto;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Pagination;

namespace Auth.Application.Admin.Services;

public interface IAdminUsersService
{
    Task<Result<PagedResult<AdminUserResponse>>> GetUsersAsync(AdminUsersQuery query, CancellationToken cancellationToken = default);
    Task<Result<AdminUserResponse>> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<OrganizerApplicationResponse>>> GetOrganizerApplicationsAsync(OrganizerApplicationsQuery query, CancellationToken cancellationToken = default);
    Task<Result<OrganizerApplicationResponse>> GetOrganizerApplicationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<OrganizerApplicationResponse>> ApproveOrganizerApplicationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<OrganizerApplicationResponse>> RejectOrganizerApplicationAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<AdminUserResponse>> CreateUserAsync(CreateAdminUserRequest request, Guid adminUserId, CancellationToken cancellationToken = default);
    Task<Result<AdminUserResponse>> ApproveOrganizerAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<AdminUserResponse>> RejectOrganizerAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserAsync(Guid userId, Guid currentAdminUserId, CancellationToken cancellationToken = default);
    Task<Result<ResetPasswordResponse>> ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<AdminUserResponse>> UpdateRoleAsync(Guid userId, UpdateUserRoleRequest request, CancellationToken cancellationToken = default);
}
