namespace TournamentPlatform.Contracts.Events;

public sealed record MatchCompletedPlayerDto
{
    public Guid UserId { get; init; }
    public string Nickname { get; init; } = default!;
    public int EloBeforeMatch { get; init; }
}
