namespace TournamentPlatform.Contracts.Events;

public sealed record MatchCompletedPlayerDto
{
    public Guid UserId { get; init; }
}
