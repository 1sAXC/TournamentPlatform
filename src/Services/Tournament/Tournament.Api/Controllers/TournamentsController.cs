using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tournament.Application.Brackets;
using Tournament.Application.Matches;
using Tournament.Application.Tournaments;
using Tournament.Application.Tournaments.Dto;
using Tournament.Application.Tournaments.Services;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Security;

namespace Tournament.Api.Controllers;

[ApiController]
[Route("api/tournaments")]
public sealed class TournamentsController(
    ITournamentService tournamentService,
    ISwissRoundService swissRoundService,
    IMatchResultService matchResultService) : ControllerBase
{
    [Authorize(Policy = AuthorizationPolicies.RequireActiveOrganizer)]
    [HttpPost]
    [ProducesResponseType(typeof(TournamentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        CreateTournamentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await tournamentService.CreateAsync(request, currentUser, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<TournamentListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await tournamentService.GetAllAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TournamentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await tournamentService.GetByIdAsync(id, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TournamentListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailable(CancellationToken cancellationToken)
    {
        var result = await tournamentService.GetAvailableAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TournamentListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken cancellationToken)
    {
        var result = await tournamentService.GetActiveAsync(cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("completed")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TournamentListItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompleted(CancellationToken cancellationToken)
    {
        var result = await tournamentService.GetCompletedAsync(cancellationToken);
        return Ok(result.Value);
    }

    [Authorize(Policy = AuthorizationPolicies.RequirePlayer)]
    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyCollection<TournamentListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMy(CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await tournamentService.GetMyAsync(currentUser, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.RequirePlayer)]
    [HttpPost("{tournamentId:guid}/registration")]
    [ProducesResponseType(typeof(TournamentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await tournamentService.RegisterPlayerAsync(tournamentId, currentUser, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.RequirePlayer)]
    [HttpDelete("{tournamentId:guid}/registration")]
    [ProducesResponseType(typeof(TournamentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Leave(Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await tournamentService.LeaveAsync(tournamentId, currentUser, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireOrganizerOrAdmin)]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TournamentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateTournamentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await tournamentService.UpdateAsync(id, request, currentUser, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireOrganizerOrAdmin)]
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(TournamentDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await tournamentService.CancelAsync(id, currentUser, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize(Policy = AuthorizationPolicies.RequireOrganizerOrAdmin)]
    [HttpPost("{tournamentId:guid}/swiss/next-round")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateNextSwissRound(Guid tournamentId, CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await swissRoundService.CreateNextRoundAsync(tournamentId, currentUser, cancellationToken);
        if (result.IsSuccess)
        {
            return NoContent();
        }

        if (result.Error == TournamentErrors.TournamentNotFound)
        {
            return NotFound(CreateProblemDetails(result.Error, StatusCodes.Status404NotFound));
        }

        if (result.Error == TournamentErrors.AccessDenied)
        {
            return Forbid();
        }

        return BadRequest(CreateProblemDetails(result.Error, StatusCodes.Status400BadRequest));
    }

    [Authorize(Policy = AuthorizationPolicies.RequireOrganizerOrAdmin)]
    [HttpPost("{tournamentId:guid}/matches/{matchId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CompleteMatch(
        Guid tournamentId,
        Guid matchId,
        CompleteMatchRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await matchResultService.CompleteMatchAsync(
            tournamentId,
            matchId,
            request,
            currentUser,
            cancellationToken);

        return ToActionResult(result);
    }

    [Authorize]
    [HttpGet("{tournamentId:guid}/matches/{matchId:guid}")]
    [ProducesResponseType(typeof(MatchDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMatchDetails(
        Guid tournamentId,
        Guid matchId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await tournamentService.GetMatchDetailsAsync(
            tournamentId,
            matchId,
            currentUser,
            cancellationToken);

        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Error == TournamentErrors.TournamentNotFound
            || result.Error == TournamentErrors.ParticipantNotFound)
        {
            return NotFound(CreateProblemDetails(result.Error, StatusCodes.Status404NotFound));
        }

        if (result.Error == TournamentErrors.AccessDenied
            || result.Error == TournamentErrors.PlayerAccessDenied
            || result.Error == TournamentErrors.AdminAccessDenied)
        {
            return Forbid();
        }

        if (result.Error == TournamentErrors.DuplicateTitle
            || result.Error == TournamentErrors.DuplicateRegistration
            || result.Error == TournamentErrors.TournamentFull
            || result.Error == TournamentErrors.RegistrationConflict
            || result.Error == TournamentErrors.CannotCancelCompleted
            || result.Error == TournamentErrors.MatchAlreadyCompleted)
        {
            return Conflict(CreateProblemDetails(result.Error, StatusCodes.Status409Conflict));
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
