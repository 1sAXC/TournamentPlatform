namespace TournamentPlatform.Contracts.Events;

public sealed record UserDeletedEvent : IntegrationEvent
{
    public UserDeletedEvent()
    {
        EventType = nameof(UserDeletedEvent);
    }

    public Guid UserId { get; init; }
    public string Email { get; init; } = default!;
    public DateTime DeletedAtUtc { get; init; }
}
