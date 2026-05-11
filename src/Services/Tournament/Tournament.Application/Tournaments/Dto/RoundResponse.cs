namespace Tournament.Application.Tournaments.Dto;

public sealed record RoundResponse(
    Guid Id,
    int Number,
    string BracketType,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    IReadOnlyCollection<MatchResponse> Matches);
