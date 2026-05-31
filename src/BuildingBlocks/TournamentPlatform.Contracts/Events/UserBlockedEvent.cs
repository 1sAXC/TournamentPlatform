namespace TournamentPlatform.Contracts.Events;

public sealed record UserBlockedEvent : IntegrationEvent
{
    public UserBlockedEvent()
    {
        EventType = nameof(UserBlockedEvent);
    }

    public Guid UserId { get; init; }
    public string Email { get; init; } = default!;
    public DateTime BlockedAtUtc { get; init; }
}
