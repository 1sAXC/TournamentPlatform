namespace TournamentPlatform.Contracts.Events;

public sealed record TournamentCompletedEvent : IntegrationEvent
{
    public TournamentCompletedEvent()
    {
        EventType = nameof(TournamentCompletedEvent);
    }

    public Guid TournamentId { get; init; }
    public string TournamentName { get; init; } = default!;
    public string DisciplineCode { get; init; } = default!;
    public DateTime CompletedAtUtc { get; init; }
    public IReadOnlyCollection<TournamentStandingDto> Standings { get; init; } = [];
}
