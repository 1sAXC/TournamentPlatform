namespace Tournament.Application.Tournaments.Dto;

public sealed record TournamentListItemResponse(
    Guid Id,
    string Title,
    string? Description,
    string DisciplineCode,
    string Format,
    int? SwissRounds,
    int TeamSize,
    int MaxPlayers,
    Guid OrganizerId,
    string Status,
    int CurrentRoundNumber,
    int ActiveParticipantsCount,
    int CurrentPlayersCount,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc);
