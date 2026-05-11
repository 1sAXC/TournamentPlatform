namespace TournamentPlatform.Contracts.Events;

public sealed record TournamentStandingDto
{
    public Guid TeamId { get; init; }
    public string TeamName { get; init; } = default!;
    public int Place { get; init; }
    public int Wins { get; init; }
    public int Losses { get; init; }
    public IReadOnlyCollection<EventTeamMemberDto> Members { get; init; } = [];
}
