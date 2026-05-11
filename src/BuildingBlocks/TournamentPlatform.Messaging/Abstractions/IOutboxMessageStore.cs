using TournamentPlatform.Messaging.Outbox;

namespace TournamentPlatform.Messaging.Abstractions;

public interface IOutboxMessageStore
{
    Task<IReadOnlyCollection<OutboxMessage>> GetUnprocessedAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(Guid outboxMessageId, DateTime processedAtUtc, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid outboxMessageId, string error, CancellationToken cancellationToken = default);
}
