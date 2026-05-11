using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Rating.Application.Ratings;
using Rating.Application.Ratings.Dto;
using Rating.Application.Ratings.Services;

namespace Rating.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/ratings")]
public sealed class RatingsController(IRatingService ratingService) : ControllerBase
{
    [HttpGet("players/{playerId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyCollection<PlayerRatingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayerRatings(Guid playerId, CancellationToken cancellationToken)
    {
        var result = await ratingService.GetPlayerRatingsAsync(playerId, cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("players/{playerId:guid}/disciplines/{disciplineCode}")]
    [ProducesResponseType(typeof(PlayerRatingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerRating(
        Guid playerId,
        string disciplineCode,
        CancellationToken cancellationToken)
    {
        var result = await ratingService.GetPlayerRatingAsync(playerId, disciplineCode, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = result.Error.Message,
            Type = result.Error.Code
        });
    }

    [HttpGet("players/{playerId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyCollection<RatingHistoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayerHistory(Guid playerId, CancellationToken cancellationToken)
    {
        var result = await ratingService.GetPlayerHistoryAsync(playerId, cancellationToken);
        return Ok(result.Value);
    }
}
