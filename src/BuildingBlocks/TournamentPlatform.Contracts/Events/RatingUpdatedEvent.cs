namespace TournamentPlatform.Contracts.Events;

public sealed record RatingUpdatedEvent : IntegrationEvent
{
    public RatingUpdatedEvent()
    {
        EventType = nameof(RatingUpdatedEvent);
    }

    public Guid UserId { get; init; }
    public string DisciplineCode { get; init; } = default!;
    public int NewElo { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
