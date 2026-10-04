using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Infrastructure.Persistence.Configurations;

public class TourInclusionConfiguration : IEntityTypeConfiguration<TourInclusion>
{
    public void Configure(EntityTypeBuilder<TourInclusion> builder)
    {
        builder.ToTable("TourInclusions");

        builder.HasKey(i => i.Id);
        // Id ставит домен (Guid.NewGuid()). ValueGeneratedOnAdd заставляет EF считать
        // новый пункт уже существующей строкой и делать UPDATE вместо INSERT.
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Text)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(i => i.Kind)
            .HasConversion(
                v => v.ToString(),
                v => (TourInclusionKind)Enum.Parse(typeof(TourInclusionKind), v))
            .HasMaxLength(32)
            .IsRequired();

        builder.HasIndex(i => i.TourId);

        builder.HasOne<TourComponent>()
            .WithMany()
            .HasForeignKey(i => i.ComponentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
