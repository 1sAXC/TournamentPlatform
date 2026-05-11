namespace TournamentPlatform.Contracts.Events;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public string EventType { get; init; } = default!;
    public string? CorrelationId { get; init; }
    public int Version { get; init; } = 1;
}
