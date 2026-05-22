namespace Tournament.Application.Tournaments.Dto;

public sealed record AdminCreateTournamentRequest(
    Guid OrganizerId,
    string Title,
    string? Description,
    string DisciplineCode,
    string Format,
    int? SwissRounds,
    int TeamSize,
    int MaxPlayers)
{
    public CreateTournamentRequest ToCreateTournamentRequest()
    {
        return new CreateTournamentRequest(
            Title,
            Description,
            DisciplineCode,
            Format,
            SwissRounds,
            TeamSize,
            MaxPlayers);
    }
}
