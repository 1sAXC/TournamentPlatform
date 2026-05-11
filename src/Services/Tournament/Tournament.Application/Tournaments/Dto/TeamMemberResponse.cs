namespace Tournament.Application.Tournaments.Dto;

public sealed record TeamMemberResponse(
    Guid PlayerId,
    string Nickname,
    int Elo);
