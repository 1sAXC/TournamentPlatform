using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches");

        builder.HasKey(match => match.Id);

        builder.Property(match => match.Id)
            .ValueGeneratedNever();

        builder.Property(match => match.Status)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(match => match.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(match => match.TeamAId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(match => match.TeamBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(match => match.TournamentId);
        builder.HasIndex(match => match.RoundId);
        builder.HasIndex(match => match.Status);
    }
}
