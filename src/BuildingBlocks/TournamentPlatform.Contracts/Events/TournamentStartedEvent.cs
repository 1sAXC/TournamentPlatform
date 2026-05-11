namespace TournamentPlatform.Contracts.Events;

public sealed record TournamentStartedEvent : IntegrationEvent
{
    public TournamentStartedEvent()
    {
        EventType = nameof(TournamentStartedEvent);
    }

    public Guid TournamentId { get; init; }
    public Guid OrganizerId { get; init; }
    public string TournamentName { get; init; } = default!;
    public string DisciplineCode { get; init; } = default!;
    public string Format { get; init; } = default!;
    public string TournamentFormat { get; init; } = default!;
    public int TeamSize { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public IReadOnlyCollection<EventTeamDto> Teams { get; init; } = [];
    public IReadOnlyCollection<EventRoundDto> Rounds { get; init; } = [];
}
