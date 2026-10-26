using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Infrastructure.Persistence.Configurations;

public class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.ToTable("SiteSettings");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.SiteName)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.SupportPhone)
            .HasMaxLength(20);

        builder.Property(x => x.ContactEmail)
            .HasMaxLength(254);

        builder.Property(x => x.OfficeAddress)
            .HasMaxLength(300);

        builder.Property(x => x.WorkingHours)
            .HasMaxLength(200);

        builder.Property(x => x.SocialLinksJson)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(x => x.BannerText)
            .HasMaxLength(500);

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasData(new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            SiteName = SiteSettings.DefaultSiteName,
            SupportPhone = null,
            ContactEmail = null,
            OfficeAddress = null,
            WorkingHours = null,
            SocialLinksJson = "[]",
            BannerText = null,
            BannerEnabled = false,
            UpdatedAt = SiteSettings.SeedUpdatedAt,
            UpdatedByUserId = null
        });
    }
}
