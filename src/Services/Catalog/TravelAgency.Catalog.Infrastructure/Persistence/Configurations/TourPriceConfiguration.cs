using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Infrastructure.Persistence.Configurations;

public class TourPriceConfiguration : IEntityTypeConfiguration<TourPrice>
{
    public void Configure(EntityTypeBuilder<TourPrice> builder)
    {
        builder.ToTable("TourPrices");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PricePerPerson)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("USD");
    }
}
