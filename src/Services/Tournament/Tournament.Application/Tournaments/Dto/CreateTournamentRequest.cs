namespace Tournament.Application.Tournaments.Dto;

public sealed record CreateTournamentRequest(
    string Title,
    string? Description,
    string DisciplineCode,
    string Format,
    int? SwissRounds,
    int TeamSize,
    int MaxPlayers);
