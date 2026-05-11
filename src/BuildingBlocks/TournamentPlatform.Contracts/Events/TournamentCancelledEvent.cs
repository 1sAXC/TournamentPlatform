namespace TournamentPlatform.Contracts.Events;

public sealed record TournamentCancelledEvent : IntegrationEvent
{
    public TournamentCancelledEvent()
    {
        EventType = nameof(TournamentCancelledEvent);
    }

    public Guid TournamentId { get; init; }
    public string TournamentName { get; init; } = default!;
    public string DisciplineCode { get; init; } = default!;
    public string? Reason { get; init; }
    public DateTime CancelledAtUtc { get; init; }
}
