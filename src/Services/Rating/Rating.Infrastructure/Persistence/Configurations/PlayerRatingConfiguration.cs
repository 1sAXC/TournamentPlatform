using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rating.Domain.Ratings;

namespace Rating.Infrastructure.Persistence.Configurations;

public sealed class PlayerRatingConfiguration : IEntityTypeConfiguration<PlayerRating>
{
    public void Configure(EntityTypeBuilder<PlayerRating> builder)
    {
        builder.ToTable("PlayerRatings");

        builder.HasKey(rating => rating.Id);

        builder.Property(rating => rating.DisciplineCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(rating => rating.Elo)
            .IsRequired();

        builder.Property(rating => rating.CreatedAtUtc)
            .IsRequired();

        builder.Property(rating => rating.UpdatedAtUtc)
            .IsRequired();

        // RowVersion is kept as a plain byte[] column for legacy compatibility
        // with the initial schema. It is NOT a concurrency token: nothing in
        // the domain updates it. PlayerRating updates are serialised through
        // a single-consumer RabbitMQ queue (prefetch=1), so optimistic
        // concurrency control is intentionally not used here.
        builder.Property(rating => rating.RowVersion)
            .HasDefaultValue(Array.Empty<byte>());

        builder.HasIndex(rating => new { rating.PlayerId, rating.DisciplineCode })
            .IsUnique();

        builder.HasIndex(rating => rating.PlayerId);
        builder.HasIndex(rating => rating.DisciplineCode);
        builder.HasIndex(rating => rating.IsDeleted);
    }
}
