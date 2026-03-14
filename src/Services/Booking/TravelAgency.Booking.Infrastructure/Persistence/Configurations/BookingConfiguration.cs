using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Booking.Domain.Enums;

namespace TravelAgency.Booking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Domain.Entities.Booking>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Booking> builder)
    {
        builder.ToTable("Bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.ClientId).IsRequired();
        builder.Property(b => b.TourId).IsRequired();
        builder.Property(b => b.Comment).HasMaxLength(1000);
        builder.Property(b => b.Status).IsRequired().HasConversion<int>();
        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt);

        builder.HasMany(b => b.Proposals)
            .WithOne()
            .HasForeignKey(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.Proposals)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(b => b.StatusHistory)
            .WithOne()
            .HasForeignKey(h => h.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(b => b.StatusHistory)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(b => b.OutboxMessages);
    }
}
