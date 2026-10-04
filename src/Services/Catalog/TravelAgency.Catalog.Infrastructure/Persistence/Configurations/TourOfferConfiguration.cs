using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Infrastructure.Persistence.Configurations;

public class TourOfferConfiguration : IEntityTypeConfiguration<TourOffer>
{
    public void Configure(EntityTypeBuilder<TourOffer> builder)
    {
        builder.ToTable("TourOffers");

        builder.HasKey(p => p.Id);
        // Id ставит домен (Guid.NewGuid()). ValueGeneratedOnAdd заставляет EF считать
        // новое предложение уже существующей строкой и делать UPDATE вместо INSERT.
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.PricePerPerson)
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("USD");

        builder.HasIndex(p => p.TourId);
    }
}
