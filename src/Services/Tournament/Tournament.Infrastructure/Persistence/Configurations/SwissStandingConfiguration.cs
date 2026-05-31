using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class SwissStandingConfiguration : IEntityTypeConfiguration<SwissStanding>
{
    public void Configure(EntityTypeBuilder<SwissStanding> builder)
    {
        builder.ToTable("SwissStandings");

        builder.HasKey(standing => standing.Id);

        builder.Property(standing => standing.Id)
            .ValueGeneratedNever();

        builder.HasIndex(standing => new { standing.TournamentId, standing.TeamId })
            .IsUnique();
    }
}
