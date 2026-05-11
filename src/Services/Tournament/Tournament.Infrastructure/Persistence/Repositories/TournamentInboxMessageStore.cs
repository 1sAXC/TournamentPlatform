using Microsoft.EntityFrameworkCore;
using TournamentPlatform.Messaging.Abstractions;
using TournamentPlatform.Messaging.Inbox;

namespace Tournament.Infrastructure.Persistence.Repositories;

public sealed class TournamentInboxMessageStore(TournamentDbContext dbContext) : IInboxMessageStore
{
    public Task<bool> HasProcessedAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default)
    {
        return dbContext.InboxMessages.AnyAsync(
            message => message.EventId == eventId && message.ConsumerName == consumerName,
            cancellationToken);
    }

    public async Task MarkProcessedAsync(
        Guid eventId,
        string consumerName,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default)
    {
        dbContext.InboxMessages.Add(new InboxMessage
        {
            EventId = eventId,
            ConsumerName = consumerName,
            ProcessedAtUtc = processedAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
