namespace Tournament.Application.Tournaments.Dto;

public sealed record UpdateTournamentRequest(
    string Title,
    string? Description);
