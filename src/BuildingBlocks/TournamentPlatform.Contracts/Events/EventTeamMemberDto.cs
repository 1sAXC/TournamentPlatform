namespace TournamentPlatform.Contracts.Events;

public sealed record EventTeamMemberDto
{
    public Guid UserId { get; init; }
    public string Nickname { get; init; } = default!;
    public int Elo { get; init; }
    public bool IsCaptain { get; init; }
}
