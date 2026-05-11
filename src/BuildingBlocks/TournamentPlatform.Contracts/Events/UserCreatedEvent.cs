namespace TournamentPlatform.Contracts.Events;

public sealed record UserCreatedEvent : IntegrationEvent
{
    public UserCreatedEvent()
    {
        EventType = nameof(UserCreatedEvent);
    }

    public Guid UserId { get; init; }
    public string Role { get; init; } = default!;
    public string Email { get; init; } = default!;
    public DateTime CreatedAtUtc { get; init; }
    public string CreationSource { get; init; } = default!;
    public string? PlayerNickname { get; init; }
    public string? OrganizerName { get; init; }
}
