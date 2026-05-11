namespace TournamentPlatform.Contracts.Events;

public sealed record RatingUpdatedEvent : IntegrationEvent
{
    public RatingUpdatedEvent()
    {
        EventType = nameof(RatingUpdatedEvent);
    }

    public Guid UserId { get; init; }
    public string DisciplineCode { get; init; } = default!;
    public int PreviousElo { get; init; }
    public int OldElo { get; init; }
    public int NewElo { get; init; }
    public int Delta { get; init; }
    public Guid? SourceMatchId { get; init; }
    public Guid? MatchId { get; init; }
    public Guid? TournamentId { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
