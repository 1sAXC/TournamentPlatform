namespace Tournament.Application.Matches;

public sealed record CompleteMatchRequest(
    Guid WinnerTeamId,
    int? WinnerScore,
    int? LoserScore,
    bool IsTechnicalDefeat);
