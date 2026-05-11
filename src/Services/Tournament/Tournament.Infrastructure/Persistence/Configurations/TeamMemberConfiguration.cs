using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tournament.Domain.Tournaments;

namespace Tournament.Infrastructure.Persistence.Configurations;

public sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("TeamMembers");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.Id)
            .ValueGeneratedNever();

        builder.Property(member => member.Nickname)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(member => member.Elo)
            .IsRequired();

        builder.HasIndex(member => new { member.TeamId, member.PlayerId })
            .IsUnique();

        builder.HasIndex(member => member.PlayerId);
    }
}
