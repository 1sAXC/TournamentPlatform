namespace TournamentPlatform.Contracts.Events;

public sealed record EventMatchDto
{
    public Guid MatchId { get; init; }
    public int MatchNumber { get; init; }
    public Guid? TeamAId { get; init; }
    public Guid? TeamBId { get; init; }
}
