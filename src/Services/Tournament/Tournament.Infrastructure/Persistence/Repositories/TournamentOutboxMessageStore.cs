using Microsoft.EntityFrameworkCore;
using TournamentPlatform.Messaging.Abstractions;
using TournamentPlatform.Messaging.Outbox;

namespace Tournament.Infrastructure.Persistence.Repositories;

public sealed class TournamentOutboxMessageStore(TournamentDbContext dbContext) : IOutboxMessageStore
{
    public async Task<IReadOnlyCollection<OutboxMessage>> GetUnprocessedAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(
        Guid outboxMessageId,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var message = await dbContext.OutboxMessages
            .FirstAsync(message => message.Id == outboxMessageId, cancellationToken);

        message.ProcessedAtUtc = processedAtUtc;
        message.Error = null;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid outboxMessageId,
        string error,
        CancellationToken cancellationToken = default)
    {
        var message = await dbContext.OutboxMessages
            .FirstAsync(message => message.Id == outboxMessageId, cancellationToken);

        message.RetryCount++;
        message.Error = error;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
