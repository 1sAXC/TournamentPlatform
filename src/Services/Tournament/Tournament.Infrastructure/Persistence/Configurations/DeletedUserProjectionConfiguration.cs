using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class DeletedUserProjectionConfiguration : IEntityTypeConfiguration<DeletedUserProjection>
{
    public void Configure(EntityTypeBuilder<DeletedUserProjection> builder)
    {
        builder.ToTable("DeletedUserProjections");

        builder.HasKey(projection => projection.UserId);

        builder.Property(projection => projection.DeletedAtUtc)
            .IsRequired();
    }
}
