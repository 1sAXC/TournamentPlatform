namespace Tournament.Application.Tournaments.Dto;

public sealed record MatchResponse(
    Guid Id,
    int MatchNumber,
    Guid? TeamAId,
    Guid? TeamBId,
    Guid? WinnerTeamId,
    Guid? LoserTeamId,
    string Status,
    int? WinnerScore,
    int? LoserScore,
    bool IsTechnicalDefeat,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);
