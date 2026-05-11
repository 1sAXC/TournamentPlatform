namespace TournamentPlatform.Messaging.Inbox;

public sealed class InboxMessage
{
    public Guid EventId { get; init; }
    public string ConsumerName { get; init; } = default!;
    public DateTime ProcessedAtUtc { get; init; }
}
