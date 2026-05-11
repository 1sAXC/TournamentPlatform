namespace TournamentPlatform.Contracts.Events;

public sealed record PlayerRegisteredToTournamentEvent : IntegrationEvent
{
    public PlayerRegisteredToTournamentEvent()
    {
        EventType = nameof(PlayerRegisteredToTournamentEvent);
    }

    public Guid TournamentId { get; init; }
    public Guid PlayerId { get; init; }
    public string PlayerNickname { get; init; } = default!;
    public string DisciplineCode { get; init; } = default!;
    public DateTime RegisteredAtUtc { get; init; }
}
