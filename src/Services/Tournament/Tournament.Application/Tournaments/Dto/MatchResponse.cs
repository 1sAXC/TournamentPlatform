namespace Tournament.Application.Tournaments.Dto;

public sealed record MatchResponse(
    Guid Id,
    int MatchNumber,
    Guid? TeamAId,
    Guid? TeamBId,
    Guid? WinnerTeamId,
    Guid? LoserTeamId,
    string Status,
    /// <summary>Rounds — sum across all maps. Used for ELO weighting.</summary>
    int? WinnerScore,
    int? LoserScore,
    /// <summary>Maps won in the series (e.g. 2 / 1 for Bo3). Display-only.</summary>
    int? WinnerMaps,
    int? LoserMaps,
    bool IsTechnicalDefeat,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);
