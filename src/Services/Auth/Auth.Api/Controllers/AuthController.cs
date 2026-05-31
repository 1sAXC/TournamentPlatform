using System.Security.Claims;
using Auth.Application.Auth;
using Auth.Application.Auth.Dto;
using Auth.Application.Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TournamentPlatform.Shared.Common;
using TournamentPlatform.Shared.Security;

namespace Auth.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register/player")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterPlayer(
        RegisterPlayerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterPlayerAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("register/organizer")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterOrganizer(
        RegisterOrganizerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterOrganizerAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await authService.GetCurrentUserAsync(currentUser.UserId, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await authService.ChangePasswordAsync(currentUser.UserId, request, cancellationToken);
        return result.IsSuccess ? NoContent() : ToErrorActionResult(result.Error);
    }

    [Authorize]
    [HttpPut("contact-handle")]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateContactHandle(
        UpdateContactHandleRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetCurrentUser(out var currentUser))
        {
            return Unauthorized();
        }

        var result = await authService.UpdateContactHandleAsync(currentUser.UserId, request, cancellationToken);
        return ToActionResult(result);
    }

    private IActionResult ToActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        if (result.Error == AuthErrors.DuplicateEmail || result.Error == AuthErrors.DuplicateNickname)
        {
            return Conflict(CreateProblemDetails(result.Error, StatusCodes.Status409Conflict));
        }

        if (result.Error == AuthErrors.InvalidCredentials || result.Error == AuthErrors.AccessDenied)
        {
            return Unauthorized(CreateProblemDetails(result.Error, StatusCodes.Status401Unauthorized));
        }

        if (result.Error == AuthErrors.UserNotFound)
        {
            return NotFound(CreateProblemDetails(result.Error, StatusCodes.Status404NotFound));
        }

        return BadRequest(CreateProblemDetails(result.Error, StatusCodes.Status400BadRequest));
    }

    private IActionResult ToErrorActionResult(Error error)
    {
        if (error == AuthErrors.InvalidCredentials || error == AuthErrors.AccessDenied)
        {
            return Unauthorized(CreateProblemDetails(error, StatusCodes.Status401Unauthorized));
        }

        if (error == AuthErrors.UserNotFound)
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
