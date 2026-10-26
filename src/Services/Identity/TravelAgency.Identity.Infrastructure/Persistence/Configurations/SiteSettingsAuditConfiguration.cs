using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Infrastructure.Persistence.Configurations;

public class SiteSettingsAuditConfiguration : IEntityTypeConfiguration<SiteSettingsAudit>
{
    public void Configure(EntityTypeBuilder<SiteSettingsAudit> builder)
    {
        builder.ToTable("SiteSettingsAudit");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldName)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.OldValue)
            .HasMaxLength(8000);

        builder.Property(x => x.NewValue)
            .HasMaxLength(8000);

        builder.Property(x => x.ChangedByUserId)
            .IsRequired();

        builder.Property(x => x.ChangedAt)
            .IsRequired();

        builder.HasIndex(x => x.ChangedAt);
    }
}
