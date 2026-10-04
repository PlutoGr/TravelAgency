using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Infrastructure.Persistence.Configurations;

public class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.ToTable("Tours");

        builder.HasKey(t => t.Id);

        builder.Ignore(t => t.IsActive);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.ShortDescription)
            .HasMaxLength(TourContentLimits.ShortDescriptionMaxLength);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(t => t.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.DepartureCity)
            .HasMaxLength(100);

        builder.Property(t => t.TourType)
            .HasConversion(
                v => v.ToString(),
                v => (TourType)Enum.Parse(typeof(TourType), v))
            .HasMaxLength(50);

        builder.Property(t => t.MealPlan)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString() : null,
                v => string.IsNullOrEmpty(v) ? null : (MealPlan)Enum.Parse(typeof(MealPlan), v))
            .HasMaxLength(8);

        builder.Property(t => t.AccommodationText)
            .HasMaxLength(4000);

        builder.Property(t => t.Status)
            .HasConversion(
                v => v.ToString(),
                v => (TourStatus)Enum.Parse(typeof(TourStatus), v))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(t => t.Source)
            .HasConversion(
                v => v.ToString(),
                v => (TourSource)Enum.Parse(typeof(TourSource), v))
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(t => t.ImageUrl)
            .HasMaxLength(500);

        builder.Navigation(t => t.Offers)
            .HasField("_offers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(t => t.Days)
            .HasField("_days")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(t => t.Inclusions)
            .HasField("_inclusions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(t => t.Images)
            .HasField("_images")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Offers)
            .WithOne()
            .HasForeignKey(o => o.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Days)
            .WithOne()
            .HasForeignKey(d => d.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Inclusions)
            .WithOne()
            .HasForeignKey(i => i.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Images)
            .WithOne()
            .HasForeignKey(i => i.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Country);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.TourType);
        builder.HasIndex(t => t.Source);
    }
}
