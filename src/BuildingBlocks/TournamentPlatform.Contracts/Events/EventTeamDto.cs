namespace TournamentPlatform.Contracts.Events;

public sealed record EventTeamDto
{
    public Guid TeamId { get; init; }
    public string Name { get; init; } = default!;
    public Guid CaptainUserId { get; init; }
    public IReadOnlyCollection<EventTeamMemberDto> Members { get; init; } = [];
}
