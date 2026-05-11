using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class RoundConfiguration : IEntityTypeConfiguration<Round>
{
    public void Configure(EntityTypeBuilder<Round> builder)
    {
        builder.ToTable("Rounds");

        builder.HasKey(round => round.Id);

        builder.Property(round => round.Id)
            .ValueGeneratedNever();

        builder.Property(round => round.BracketType)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(round => round.Status)
            .HasConversion<string>()
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(round => round.CreatedAtUtc)
            .IsRequired();

        builder.HasMany(round => round.Matches)
            .WithOne()
            .HasForeignKey(match => match.RoundId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(round => new { round.TournamentId, round.Number, round.BracketType });
    }
}
