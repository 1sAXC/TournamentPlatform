namespace Tournament.Application.Tournaments.Dto;

public sealed record MatchDetailsResponse(
    Guid TournamentId,
    string TournamentTitle,
    string? TournamentDescription,
    string DisciplineCode,
    string Format,
    int TeamSize,
    string TournamentStatus,
    Guid MatchId,
    int MatchNumber,
    int RoundNumber,
    string MatchStatus,
    /// <summary>Rounds — sum across all maps. ELO weighting input.</summary>
    int? WinnerScore,
    int? LoserScore,
    /// <summary>Maps won in the series (e.g. 2 / 1 for Bo3). Display-only.</summary>
    int? WinnerMaps,
    int? LoserMaps,
    Guid? WinnerTeamId,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    MatchOrganizerResponse Organizer,
    MatchTeamResponse? TeamA,
    MatchTeamResponse? TeamB,
    bool CanSeeContacts);

public sealed record MatchOrganizerResponse(
    Guid OrganizerId,
    string? OrganizerName,
    string? ContactHandle);

public sealed record MatchTeamResponse(
    Guid Id,
    string Name,
    Guid CaptainPlayerId,
    int Seed,
    double AverageElo,
    IReadOnlyCollection<MatchTeamMemberResponse> Members);

public sealed record MatchTeamMemberResponse(
    Guid PlayerId,
    string Nickname,
    int Elo,
    bool IsCaptain,
    string? ContactHandle);
