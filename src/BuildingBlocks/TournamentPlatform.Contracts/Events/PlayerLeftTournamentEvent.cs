namespace TournamentPlatform.Contracts.Events;

public sealed record PlayerLeftTournamentEvent : IntegrationEvent
{
    public PlayerLeftTournamentEvent()
    {
        EventType = nameof(PlayerLeftTournamentEvent);
    }

    public Guid TournamentId { get; init; }
    public Guid PlayerId { get; init; }
    public string DisciplineCode { get; init; } = default!;
    public DateTime LeftAtUtc { get; init; }
}
