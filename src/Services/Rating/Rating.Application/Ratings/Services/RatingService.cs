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
    private const int MaxScoreDifference = 10;

    private static readonly IReadOnlyCollection<string> InitialDisciplines =
    [
        DisciplineCodes.CS2,
        DisciplineCodes.Valorant,
        DisciplineCodes.Standoff2
    ];

    public async Task HandleUserCreatedAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(integrationEvent.Role, "Player", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Load ALL ratings including soft-deleted ones — this method serves
        // both first-time creation AND unblock-restoration (User.Unblock
        // re-emits UserCreatedEvent with CreationSource="Unblock"). If we
        // filtered out soft-deleted rows we'd try to INSERT a duplicate and
        // violate the unique index (PlayerId, DisciplineCode).
        var existingRatings = await ratings.GetAllPlayerRatingsAsync(integrationEvent.UserId, cancellationToken);
        var existingByCode = existingRatings.ToDictionary(
            rating => rating.DisciplineCode,
            StringComparer.OrdinalIgnoreCase);

        var now = DateTime.UtcNow;
        var anyChange = false;

        foreach (var disciplineCode in InitialDisciplines)
        {
            if (existingByCode.TryGetValue(disciplineCode, out var existing))
            {
                if (existing.IsDeleted)
                {
                    existing.Restore(now);
                    anyChange = true;
                }
                continue;
            }

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
            anyChange = true;
        }

        if (anyChange)
        {
            await ratings.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task HandleUserBlockedAsync(UserBlockedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        // Internally the Rating subdomain still flags affected per-player rows
        // as "deleted" — that column predates the block/unblock naming and is
        // not exposed externally. Renaming requires a column-level migration
        // which is intentionally out of scope for this change.
        var playerRatings = await ratings.GetPlayerRatingsAsync(integrationEvent.UserId, cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var playerRating in playerRatings)
        {
            playerRating.MarkDeleted(now);
        }

        await ratings.SaveChangesAsync(cancellationToken);
    }

    public Task HandleUserRoleChangedAsync(
        UserRoleChangedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(integrationEvent.NewRole, "Player", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        return HandleUserCreatedAsync(new UserCreatedEvent
        {
            UserId = integrationEvent.UserId,
            Role = integrationEvent.NewRole,
            Email = string.Empty,
            CreatedAtUtc = integrationEvent.ChangedAtUtc,
            CreationSource = "RoleChange",
            PlayerNickname = integrationEvent.Nickname
        }, cancellationToken);
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
        var winnerDelta = eloCalculator.CalculateTeamDelta(
            winnerAverageElo,
            loserAverageElo,
            integrationEvent.TeamSize,
            actualScore: 1,
            scoreCoefficient);
        var loserDelta = -winnerDelta;
        var now = DateTime.UtcNow;

        foreach (var player in integrationEvent.WinnerPlayers)
        {
            ApplyPlayerRatingChange(
                winnerRatings[player.UserId],
                winnerDelta,
                integrationEvent,
                isWin: true,
                now);
        }

        foreach (var player in integrationEvent.LoserPlayers)
        {
            ApplyPlayerRatingChange(
                loserRatings[player.UserId],
                loserDelta,
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
            // Use the deleted-aware lookup: if the player was blocked
            // mid-tournament the row exists with IsDeleted=true; the regular
            // GetPlayerRatingAsync would return null and we'd try to insert a
            // duplicate, violating the unique (PlayerId, DisciplineCode)
            // index. Restoring the row keeps the previous history intact.
            var rating = await ratings.GetPlayerRatingIncludingDeletedAsync(player.UserId, disciplineCode, cancellationToken);
            if (rating is null)
            {
                rating = PlayerRating.CreateInitial(player.UserId, disciplineCode, InitialElo, DateTime.UtcNow);
                ratings.AddPlayerRating(rating);
            }
            else if (rating.IsDeleted)
            {
                rating.Restore(DateTime.UtcNow);
            }

            result[player.UserId] = rating;
        }

        return result;
    }

    private void ApplyPlayerRatingChange(
        PlayerRating rating,
        int delta,
        MatchCompletedEvent integrationEvent,
        bool isWin,
        DateTime updatedAtUtc)
    {
        var oldElo = rating.Elo;
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
            NewElo = newElo,
            UpdatedAtUtc = updatedAtUtc
        });
    }

    private static double CalculateScoreCoefficient(MatchCompletedEvent integrationEvent)
    {
        int scoreDifference;

        if (integrationEvent.IsTechnicalDefeat)
        {
            scoreDifference = MaxScoreDifference;
        }
        else if (integrationEvent.WinnerScore is not null && integrationEvent.LoserScore is not null)
        {
            scoreDifference = Math.Abs(integrationEvent.WinnerScore.Value - integrationEvent.LoserScore.Value);
        }
        else
        {
            scoreDifference = 1;
        }

        return 1 + Math.Min(scoreDifference, MaxScoreDifference) * 0.025;
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
