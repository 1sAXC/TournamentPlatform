using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class UserProjectionConfiguration : IEntityTypeConfiguration<UserProjection>
{
    public void Configure(EntityTypeBuilder<UserProjection> builder)
    {
        builder.ToTable("user_projections");

        builder.HasKey(projection => projection.UserId);

        builder.Property(projection => projection.UserId)
            .HasColumnName("user_id");

        builder.Property(projection => projection.Role)
            .HasColumnName("role")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(projection => projection.ContactHandle)
            .HasColumnName("contact_handle")
            .HasMaxLength(64);

        builder.Property(projection => projection.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(projection => projection.DeletedAtUtc)
            .HasColumnName("deleted_at_utc");
    }
}
