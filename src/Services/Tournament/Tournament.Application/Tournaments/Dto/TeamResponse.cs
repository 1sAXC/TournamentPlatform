namespace Tournament.Application.Tournaments.Dto;

public sealed record TeamResponse(
    Guid Id,
    string Name,
    Guid CaptainPlayerId,
    int Seed,
    double AverageElo,
    IReadOnlyCollection<TeamMemberResponse> Members);
