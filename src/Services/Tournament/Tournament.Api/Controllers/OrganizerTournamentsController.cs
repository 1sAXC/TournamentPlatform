using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Dto;
using Tournament.Application.Tournaments.Services;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Security;

namespace Tournament.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.RequireActiveOrganizer)]
[Route("api/organizer/tournaments")]
public sealed class OrganizerTournamentsController(ITournamentService tournamentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TournamentListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrganizerTournaments(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await tournamentService.GetOrganizerTournamentsAsync(currentUser, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : ToErrorActionResult(result.Error);
    }

    private IActionResult ToErrorActionResult(Error error)
    {
        return error == TournamentErrors.AccessDenied
            ? Forbid()
            : BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = error.Message,
                Type = error.Code
            });
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
}
