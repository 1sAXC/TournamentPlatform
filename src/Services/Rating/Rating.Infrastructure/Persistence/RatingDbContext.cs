using Microsoft.EntityFrameworkCore;
using Rating.Domain.Ratings;
using TournamentPlatform.Messaging.Inbox;
using TournamentPlatform.Messaging.Outbox;

namespace Rating.Infrastructure.Persistence;

public sealed class RatingDbContext(DbContextOptions<RatingDbContext> options) : DbContext(options)
{
    public DbSet<PlayerRating> PlayerRatings => Set<PlayerRating>();
    public DbSet<RatingHistory> RatingHistories => Set<RatingHistory>();
    public DbSet<PlayerTournamentStatistic> PlayerTournamentStatistics => Set<PlayerTournamentStatistic>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RatingDbContext).Assembly);
    }
}
