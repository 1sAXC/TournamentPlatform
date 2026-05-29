using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Notifications;
using Notification.Application.Notifications.Dto;
using Notification.Application.Notifications.Services;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Security;

namespace Notification.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(INotificationService notifications) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationListResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var list = await notifications.ListAsync(currentUser.UserId, unreadOnly, pageNumber, pageSize, cancellationToken);
        return Ok(list);
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await notifications.MarkReadAsync(id, currentUser.UserId, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return ToErrorActionResult(result.Error);
    }

    private IActionResult ToErrorActionResult(Error error)
    {
        if (error == NotificationErrors.NotFound)
        {
            return NotFound(CreateProblemDetails(error, StatusCodes.Status404NotFound));
        }

        if (error == NotificationErrors.AccessDenied)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateProblemDetails(error, StatusCodes.Status403Forbidden));
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
