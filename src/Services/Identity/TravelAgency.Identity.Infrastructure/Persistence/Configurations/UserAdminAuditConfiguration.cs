using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Infrastructure.Persistence.Configurations;

public class UserAdminAuditConfiguration : IEntityTypeConfiguration<UserAdminAudit>
{
    public void Configure(EntityTypeBuilder<UserAdminAudit> builder)
    {
        builder.ToTable("UserAdminAudit");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Id).ValueGeneratedNever();

        builder.Property(audit => audit.Action)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(audit => audit.OldRole)
            .HasMaxLength(50);

        builder.Property(audit => audit.NewRole)
            .HasMaxLength(50);

        builder.Property(audit => audit.Reason)
            .HasMaxLength(User.MaxBlockReasonLength);

        builder.Property(audit => audit.CreatedAt)
            .IsRequired();

        builder.HasIndex(audit => new { audit.TargetUserId, audit.CreatedAt });
    }
}
