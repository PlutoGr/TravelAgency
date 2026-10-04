using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Booking.Domain.Entities;

namespace TravelAgency.Booking.Infrastructure.Persistence.Configurations;

public class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
    {
        builder.ToTable("BookingStatusHistories");

        builder.HasKey(h => h.Id);
        // Id ставит домен. Запись добавляется к уже отслеживаемой брони (TransitionTo),
        // поэтому ValueGeneratedOnAdd превращает вставку в UPDATE несуществующей строки.
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.BookingId).IsRequired();
        builder.Property(h => h.Status).IsRequired().HasConversion<int>();
        builder.Property(h => h.ChangedByUserId).IsRequired();
        builder.Property(h => h.ChangedAt).IsRequired();
    }
}
