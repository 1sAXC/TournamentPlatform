namespace TournamentPlatform.Contracts.Events;

/// <summary>
/// Raised by the Auth service when a player or organizer updates their
/// contact handle (Telegram/Discord/etc.). Downstream services that keep
/// a local projection of the user (e.g. Tournament's UserProjection) use
/// this to stay in sync without HTTP calls back to Auth.
/// </summary>
public sealed record UserContactHandleChangedEvent : IntegrationEvent
{
    public UserContactHandleChangedEvent()
    {
        EventType = nameof(UserContactHandleChangedEvent);
    }

    public Guid UserId { get; init; }
    public string ContactHandle { get; init; } = default!;
    public DateTime ChangedAtUtc { get; init; }
}
