using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Booking.Domain.Entities;

namespace TravelAgency.Booking.Infrastructure.Persistence.Configurations;

public class BookingAssignmentHistoryConfiguration : IEntityTypeConfiguration<BookingAssignmentHistory>
{
    public void Configure(EntityTypeBuilder<BookingAssignmentHistory> builder)
    {
        builder.ToTable("BookingAssignmentHistories");

        builder.HasKey(h => h.Id);
        // Id ставит домен. Запись добавляется к уже отслеживаемой брони (AssignManager),
        // поэтому ValueGeneratedOnAdd превращает вставку в UPDATE несуществующей строки.
        builder.Property(h => h.Id).ValueGeneratedNever();

        builder.Property(h => h.BookingId).IsRequired();
        builder.Property(h => h.PreviousManagerId);
        builder.Property(h => h.NewManagerId).IsRequired();
        builder.Property(h => h.ChangedByUserId).IsRequired();
        builder.Property(h => h.ChangedAt).IsRequired();
        builder.Property(h => h.Comment).HasMaxLength(500);

        builder.HasIndex(h => new { h.BookingId, h.ChangedAt });

        builder.HasOne<Domain.Entities.Booking>()
            .WithMany(b => b.AssignmentHistory)
            .HasForeignKey(h => h.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        var navigation = builder.Metadata.Model
            .FindEntityType(typeof(Domain.Entities.Booking))!
            .FindNavigation(nameof(Domain.Entities.Booking.AssignmentHistory))!;
        navigation.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
