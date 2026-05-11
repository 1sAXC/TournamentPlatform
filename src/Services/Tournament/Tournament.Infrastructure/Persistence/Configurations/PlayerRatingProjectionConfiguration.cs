using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class PlayerRatingProjectionConfiguration : IEntityTypeConfiguration<PlayerRatingProjection>
{
    public void Configure(EntityTypeBuilder<PlayerRatingProjection> builder)
    {
        builder.ToTable("PlayerRatingProjections");

        builder.HasKey(rating => rating.Id);

        builder.Property(rating => rating.Id)
            .ValueGeneratedNever();

        builder.Property(rating => rating.DisciplineCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(rating => rating.Elo)
            .IsRequired();

        builder.Property(rating => rating.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(rating => new { rating.PlayerId, rating.DisciplineCode })
            .IsUnique();

        builder.HasIndex(rating => rating.PlayerId);
        builder.HasIndex(rating => rating.DisciplineCode);
    }
}
