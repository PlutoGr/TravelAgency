using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Infrastructure.Persistence.Configurations;

public class TourDayConfiguration : IEntityTypeConfiguration<TourDay>
{
    public void Configure(EntityTypeBuilder<TourDay> builder)
    {
        builder.ToTable("TourDays");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(d => d.Description)
            .IsRequired()
            .HasMaxLength(4000);

        builder.HasIndex(d => new { d.TourId, d.DayNumber })
            .IsUnique();

        builder.HasOne<TourComponent>()
            .WithMany()
            .HasForeignKey(d => d.ComponentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
