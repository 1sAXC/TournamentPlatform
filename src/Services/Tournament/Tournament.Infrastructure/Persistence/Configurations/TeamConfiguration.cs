using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");

        builder.HasKey(team => team.Id);

        builder.Property(team => team.Id)
            .ValueGeneratedNever();

        builder.Property(team => team.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(team => team.Seed)
            .IsRequired();

        builder.Property(team => team.AverageElo)
            .IsRequired();

        builder.HasMany(team => team.Members)
            .WithOne()
            .HasForeignKey(member => member.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(team => team.TournamentId);
        builder.HasIndex(team => new { team.TournamentId, team.Name })
            .IsUnique();
    }
}
