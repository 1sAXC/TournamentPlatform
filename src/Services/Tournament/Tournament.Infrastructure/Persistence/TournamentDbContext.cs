using Microsoft.EntityFrameworkCore;
using Tournament.Domain.Tournaments;
using TournamentPlatform.Messaging.Inbox;
using TournamentPlatform.Messaging.Outbox;

namespace Tournament.Infrastructure.Persistence;

public sealed class TournamentDbContext(DbContextOptions<TournamentDbContext> options) : DbContext(options)
{
    public DbSet<Domain.Tournaments.Tournament> Tournaments => Set<Domain.Tournaments.Tournament>();
    public DbSet<TournamentParticipant> TournamentParticipants => Set<TournamentParticipant>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<SwissStanding> SwissStandings => Set<SwissStanding>();
    public DbSet<DoubleEliminationStanding> DoubleEliminationStandings => Set<DoubleEliminationStanding>();
    public DbSet<Discipline> Disciplines => Set<Discipline>();
    public DbSet<PlayerRatingProjection> PlayerRatingProjections => Set<PlayerRatingProjection>();
    public DbSet<UserProjection> UserProjections => Set<UserProjection>();
    public DbSet<DeletedUserProjection> DeletedUserProjections => Set<DeletedUserProjection>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TournamentDbContext).Assembly);
    }
}
