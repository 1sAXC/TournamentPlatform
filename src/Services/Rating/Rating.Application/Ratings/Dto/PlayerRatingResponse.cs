namespace Rating.Application.Ratings.Dto;

public sealed record PlayerRatingResponse(
    Guid Id,
    Guid PlayerId,
    string DisciplineCode,
    int Elo,
    int Wins,
    int Losses,
    int MatchesPlayed,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
