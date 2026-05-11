namespace Rating.Application.Ratings.Dto;

public sealed record RatingHistoryResponse(
    Guid Id,
    Guid PlayerId,
    string DisciplineCode,
    int OldElo,
    int NewElo,
    int Delta,
    Guid? MatchId,
    Guid? TournamentId,
    string Reason,
    DateTime CreatedAtUtc);
