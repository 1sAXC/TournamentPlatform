namespace TournamentPlatform.Messaging.Abstractions;

public interface IInboxMessageStore
{
    Task<bool> HasProcessedAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid eventId, string consumerName, DateTime processedAtUtc, CancellationToken cancellationToken = default);
}
