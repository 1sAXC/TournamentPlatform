namespace Tournament.Application.Tournaments.Dto;

public sealed record TournamentParticipantResponse(
    Guid Id,
    Guid PlayerId,
    string PlayerNickname,
    DateTime RegisteredAtUtc,
    DateTime? LeftAtUtc,
    bool IsActive);
