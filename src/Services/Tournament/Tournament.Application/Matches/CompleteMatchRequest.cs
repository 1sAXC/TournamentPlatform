namespace Tournament.Application.Matches;

/// <summary>
/// Result of a match.
/// WinnerScore / LoserScore — rounds (sum across all maps in the series),
/// used by the Rating service to weight the ELO delta.
/// WinnerMaps / LoserMaps — maps won in the series (e.g. 2 / 1 for Bo3);
/// display-only.
/// </summary>
public sealed record CompleteMatchRequest(
    Guid WinnerTeamId,
    int? WinnerScore,
    int? LoserScore,
    int? WinnerMaps,
    int? LoserMaps,
    bool IsTechnicalDefeat);
