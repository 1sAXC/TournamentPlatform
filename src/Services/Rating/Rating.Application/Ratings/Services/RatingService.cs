using Rating.Application.Ratings.Abstractions;
using Rating.Application.Ratings.Dto;
using Rating.Domain.Ratings;
using TournamentPlatform.Contracts.Common;
using TournamentPlatform.Contracts.Events;
using TournamentPlatform.Shared.Common;

namespace Rating.Application.Ratings.Services;

public sealed class RatingService(
    IRatingRepository ratings,
    IEloCalculator eloCalculator,
    IOutboxWriter outboxWriter) : IRatingService
{
    private const int InitialElo = 1000;
    private const int MinimumElo = 100;

    private static readonly IReadOnlyCollection<string> InitialDisciplines =
    [
        DisciplineCodes.CS2,
        DisciplineCodes.PUBG,
        DisciplineCodes.Valorant,
        DisciplineCodes.Standoff2
    ];

    public async Task HandleUserCreatedAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(integrationEvent.Role, "Player", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (await ratings.HasAnyRatingAsync(integrationEvent.UserId, cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var disciplineCode in InitialDisciplines)
        {
            ratings.AddPlayerRating(PlayerRating.CreateInitial(
                integrationEvent.UserId,
                disciplineCode,
                InitialElo,
                now));

            ratings.AddRatingHistory(RatingHistory.CreateInitial(
                integrationEvent.UserId,
                disciplineCode,
                InitialElo,
                now));
        }

        await ratings.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleUserDeletedAsync(UserDeletedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        var playerRatings = await ratings.GetPlayerRatingsAsync(integrationEvent.UserId, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var playerRating in playerRatings)
        {
            playerRating.MarkDeleted(now);
        }

        await ratings.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleMatchCompletedAsync(
        MatchCompletedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (await ratings.HasMatchHistoryAsync(integrationEvent.MatchId, cancellationToken))
        {
            return;
        }

        var winnerRatings = await GetOrCreateRatingsAsync(
            integrationEvent.WinnerPlayers,
            integrationEvent.DisciplineCode,
            cancellationToken);
        var loserRatings = await GetOrCreateRatingsAsync(
            integrationEvent.LoserPlayers,
            integrationEvent.DisciplineCode,
            cancellationToken);

        var winnerAverageElo = winnerRatings.Values.Average(rating => rating.Elo);
        var loserAverageElo = loserRatings.Values.Average(rating => rating.Elo);
        var scoreCoefficient = CalculateScoreCoefficient(integrationEvent);
        var now = DateTime.UtcNow;

        foreach (var player in integrationEvent.WinnerPlayers)
        {
            ApplyPlayerMatchResult(
                winnerRatings[player.UserId],
                loserAverageElo,
                actualScore: 1,
                scoreCoefficient,
                integrationEvent,
                isWin: true,
                now);
        }

        foreach (var player in integrationEvent.LoserPlayers)
        {
            ApplyPlayerMatchResult(
                loserRatings[player.UserId],
                winnerAverageElo,
                actualScore: 0,
                scoreCoefficient,
                integrationEvent,
                isWin: false,
                now);
        }

        await ratings.SaveChangesAsync(cancellationToken);
    }

    public async Task<Result<IReadOnlyCollection<PlayerRatingResponse>>> GetPlayerRatingsAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var playerRatings = await ratings.GetPlayerRatingsAsync(playerId, cancellationToken);
        return Result<IReadOnlyCollection<PlayerRatingResponse>>.Success(playerRatings.Select(ToResponse).ToArray());
    }

    public async Task<Result<PlayerRatingResponse>> GetPlayerRatingAsync(
        Guid playerId,
        string disciplineCode,
        CancellationToken cancellationToken = default)
    {
        var playerRating = await ratings.GetPlayerRatingAsync(playerId, disciplineCode, cancellationToken);
        return playerRating is null
            ? Result<PlayerRatingResponse>.Failure(RatingErrors.RatingNotFound)
            : Result<PlayerRatingResponse>.Success(ToResponse(playerRating));
    }

    public async Task<Result<IReadOnlyCollection<RatingHistoryResponse>>> GetPlayerHistoryAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var history = await ratings.GetPlayerHistoryAsync(playerId, cancellationToken);
        return Result<IReadOnlyCollection<RatingHistoryResponse>>.Success(history.Select(ToResponse).ToArray());
    }

    private static PlayerRatingResponse ToResponse(PlayerRating rating)
    {
        return new PlayerRatingResponse(
            rating.Id,
            rating.PlayerId,
            rating.DisciplineCode,
            rating.Elo,
            rating.Wins,
            rating.Losses,
            rating.MatchesPlayed,
            rating.CreatedAtUtc,
            rating.UpdatedAtUtc);
    }

    private async Task<Dictionary<Guid, PlayerRating>> GetOrCreateRatingsAsync(
        IReadOnlyCollection<MatchCompletedPlayerDto> players,
        string disciplineCode,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, PlayerRating>();
        foreach (var player in players)
        {
            var rating = await ratings.GetPlayerRatingAsync(player.UserId, disciplineCode, cancellationToken);
            if (rating is null)
            {
                rating = PlayerRating.CreateInitial(player.UserId, disciplineCode, InitialElo, DateTime.UtcNow);
                ratings.AddPlayerRating(rating);
            }

            result[player.UserId] = rating;
        }

        return result;
    }

    private void ApplyPlayerMatchResult(
        PlayerRating rating,
        double opponentAverageElo,
        double actualScore,
        double scoreCoefficient,
        MatchCompletedEvent integrationEvent,
        bool isWin,
        DateTime updatedAtUtc)
    {
        var oldElo = rating.Elo;
        var delta = eloCalculator.CalculateDelta(oldElo, opponentAverageElo, actualScore, scoreCoefficient);
        var newElo = Math.Max(MinimumElo, oldElo + delta);

        rating.ApplyMatchResult(newElo, isWin, updatedAtUtc);
        ratings.AddRatingHistory(RatingHistory.CreateMatchResult(
            rating.PlayerId,
            rating.DisciplineCode,
            oldElo,
            newElo,
            integrationEvent.MatchId,
            integrationEvent.TournamentId,
            updatedAtUtc));

        outboxWriter.Add(new RatingUpdatedEvent
        {
            UserId = rating.PlayerId,
            DisciplineCode = rating.DisciplineCode,
            PreviousElo = oldElo,
            OldElo = oldElo,
            NewElo = newElo,
            Delta = newElo - oldElo,
            SourceMatchId = integrationEvent.MatchId,
            MatchId = integrationEvent.MatchId,
            TournamentId = integrationEvent.TournamentId,
            UpdatedAtUtc = updatedAtUtc
        });
    }

    private static double CalculateScoreCoefficient(MatchCompletedEvent integrationEvent)
    {
        if (integrationEvent.IsTechnicalDefeat
            || integrationEvent.WinnerScore is null
            || integrationEvent.LoserScore is null)
        {
            return 1.0;
        }

        var scoreDifference = Math.Abs(integrationEvent.WinnerScore.Value - integrationEvent.LoserScore.Value);
        return 1 + Math.Min(scoreDifference, 10) * 0.03;
    }

    private static RatingHistoryResponse ToResponse(RatingHistory history)
    {
        return new RatingHistoryResponse(
            history.Id,
            history.PlayerId,
            history.DisciplineCode,
            history.OldElo,
            history.NewElo,
            history.Delta,
            history.MatchId,
            history.TournamentId,
            history.Reason,
            history.CreatedAtUtc);
    }
}
