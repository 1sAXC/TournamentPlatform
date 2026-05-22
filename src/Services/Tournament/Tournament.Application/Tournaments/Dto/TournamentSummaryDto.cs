using TournamentPlatform.Contracts.Enums;

namespace Tournament.Application.Tournaments.Dto;

public sealed record TournamentSummaryDto(
    Guid Id,
    string Title,
    string? Description,
    string DisciplineCode,
    TournamentFormat Format,
    int? SwissRounds,
    int TeamSize,
    int MaxPlayers,
    Guid OrganizerId,
    TournamentStatus Status,
    int CurrentRoundNumber,
    int CurrentPlayersCount,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    DateTime? CancelledAtUtc);
