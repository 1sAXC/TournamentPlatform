namespace TournamentPlatform.Contracts.Events;

public sealed record UserRoleChangedEvent : IntegrationEvent
{
    public UserRoleChangedEvent()
    {
        EventType = nameof(UserRoleChangedEvent);
    }

    public Guid UserId { get; init; }
    public string OldRole { get; init; } = default!;
    public string NewRole { get; init; } = default!;
    public string? Nickname { get; init; }
    public string? OrganizerName { get; init; }
    public DateTime ChangedAtUtc { get; init; }
}
