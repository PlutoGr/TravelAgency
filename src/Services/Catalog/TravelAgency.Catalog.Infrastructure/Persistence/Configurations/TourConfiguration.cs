using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Infrastructure.Persistence.Configurations;

public class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.ToTable("Tours");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(t => t.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.TourType)
            .HasConversion(
                v => v.ToString(),
                v => (TourType)Enum.Parse(typeof(TourType), v))
            .HasMaxLength(50);

        builder.Property(t => t.ImageUrl)
            .HasMaxLength(500);

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        builder.Navigation(t => t.Prices)
            .HasField("_prices")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(t => t.Prices)
            .WithOne()
            .HasForeignKey(p => p.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.Country);
        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.TourType);
    }
}
