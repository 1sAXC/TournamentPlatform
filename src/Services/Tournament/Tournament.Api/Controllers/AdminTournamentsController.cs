using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Services;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Security;

namespace Tournament.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireAdmin)]
[Route("api/admin/tournaments")]
public sealed class AdminTournamentsController(ITournamentService tournamentService) : ControllerBase
{
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await tournamentService.DeleteAsync(id, currentUser, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.Error == TournamentErrors.TournamentNotFound)
        {
            return NotFound(CreateProblemDetails(result.Error, StatusCodes.Status404NotFound));
        }

        if (result.Error == TournamentErrors.AdminAccessDenied)
        {
            return Forbid();
        }

        return BadRequest(CreateProblemDetails(result.Error, StatusCodes.Status400BadRequest));
    }

    private bool TryGetCurrentUser(out CurrentTournamentUser currentUser)
    {
        currentUser = default!;
        if (!User.TryGetCurrentUser(out var user))
        {
            return false;
        }

        currentUser = new CurrentTournamentUser(
            user.UserId,
            user.Role,
            user.AccountStatus ?? string.Empty,
            user.Nickname);

        return true;
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
