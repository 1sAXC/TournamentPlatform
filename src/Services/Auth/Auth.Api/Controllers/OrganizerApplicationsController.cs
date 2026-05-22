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
[Route("api/admin/organizer-applications")]
public sealed class OrganizerApplicationsController(IAdminUsersService adminUsersService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrganizerApplicationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApplications(
        [FromQuery] OrganizerApplicationsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await adminUsersService.GetOrganizerApplicationsAsync(query, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrganizerApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplication(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.GetOrganizerApplicationAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(typeof(OrganizerApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveApplication(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.ApproveOrganizerApplicationAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(typeof(OrganizerApplicationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectApplication(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminUsersService.RejectOrganizerApplicationAsync(id, cancellationToken);
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

        return BadRequest(CreateProblemDetails(error, StatusCodes.Status400BadRequest));
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
