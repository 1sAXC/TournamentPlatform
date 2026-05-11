using System.Security.Claims;
using Auth.Application.Admin;
using Auth.Application.Admin.Dto;
using Auth.Application.Admin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Pagination;
using TournamentPlatform.Shared.Security;

namespace Auth.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
[Route("api/admin")]
public sealed class AdminUsersController(IAdminUsersService adminUsersService) : ControllerBase
{
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<AdminUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] AdminUsersQuery query,
        CancellationToken cancellationToken)
    {
        var result = await adminUsersService.GetUsersAsync(query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("users/{id:guid}")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.GetUserAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("users")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser(
        CreateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentAdminId))
        {
            return Unauthorized();
        }

        var result = await adminUsersService.CreateUserAsync(request, currentAdminId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("organizers/{id:guid}/approve")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveOrganizer(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.ApproveOrganizerAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("organizers/{id:guid}/reject")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectOrganizer(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.RejectOrganizerAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("users/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var currentAdminId))
        {
            return Unauthorized();
        }

        var result = await adminUsersService.DeleteUserAsync(id, currentAdminId, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return ToErrorActionResult(result.Error);
    }

    [HttpPost("users/{id:guid}/reset-password")]
    [ProducesResponseType(typeof(ResetPasswordResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetPassword(
        Guid id,
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminUsersService.ResetPasswordAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPatch("users/{id:guid}/role")]
    [ProducesResponseType(typeof(AdminUserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminUsersService.UpdateRoleAsync(id, request, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        return result.IsSuccess ? Ok(result.Value) : ToErrorActionResult(result.Error);
    }

    private IActionResult ToErrorActionResult(Error error)
    {
        if (error == AdminErrors.UserNotFound)
        {
            return NotFound(CreateProblemDetails(error, StatusCodes.Status404NotFound));
        }

        if (error == AdminErrors.DuplicateEmail || error == AdminErrors.DuplicateNickname)
        {
            return Conflict(CreateProblemDetails(error, StatusCodes.Status409Conflict));
        }

        if (error == AdminErrors.LastAdminDeleteNotAllowed)
        {
            return Conflict(CreateProblemDetails(error, StatusCodes.Status409Conflict));
        }

        return BadRequest(CreateProblemDetails(error, StatusCodes.Status400BadRequest));
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdClaim, out userId);
    }

    private static ProblemDetails CreateProblemDetails(Error error, int statusCode)
    {
        return new ProblemDetails
        {
            Status = statusCode,
            Title = error.Message,
            Type = error.Code
        };
    }
}
