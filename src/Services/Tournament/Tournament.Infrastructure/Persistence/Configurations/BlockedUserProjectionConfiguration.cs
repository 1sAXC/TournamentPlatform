using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class BlockedUserProjectionConfiguration : IEntityTypeConfiguration<BlockedUserProjection>
{
    public void Configure(EntityTypeBuilder<BlockedUserProjection> builder)
    {
        builder.ToTable("BlockedUserProjections");

        builder.HasKey(projection => projection.UserId);

        builder.Property(projection => projection.BlockedAtUtc)
            .IsRequired();
    }
}
