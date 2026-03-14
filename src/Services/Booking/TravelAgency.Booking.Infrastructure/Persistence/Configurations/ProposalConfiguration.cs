using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Booking.Domain.Entities;

namespace TravelAgency.Booking.Infrastructure.Persistence.Configurations;

public class ProposalConfiguration : IEntityTypeConfiguration<Proposal>
{
    public void Configure(EntityTypeBuilder<Proposal> builder)
    {
        builder.ToTable("Proposals");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.BookingId).IsRequired();
        builder.Property(p => p.ManagerId).IsRequired();
        builder.Property(p => p.Notes).HasMaxLength(2000);
        builder.Property(p => p.IsConfirmed).IsRequired();
        builder.Property(p => p.CreatedAt).IsRequired();

        builder.OwnsOne(p => p.TourSnapshot, snapshot =>
        {
            snapshot.Property(s => s.TourId)
                .HasColumnName("TourSnapshot_TourId")
                .IsRequired();

            snapshot.Property(s => s.Title)
                .HasColumnName("TourSnapshot_Title")
                .HasMaxLength(500)
                .IsRequired();

            snapshot.Property(s => s.Description)
                .HasColumnName("TourSnapshot_Description")
                .HasMaxLength(5000)
                .IsRequired();

            snapshot.Property(s => s.Price)
                .HasColumnName("TourSnapshot_Price")
                .HasPrecision(18, 2)
                .IsRequired();

            snapshot.Property(s => s.Currency)
                .HasColumnName("TourSnapshot_Currency")
                .HasMaxLength(10)
                .IsRequired();

            snapshot.Property(s => s.DurationDays)
                .HasColumnName("TourSnapshot_DurationDays")
                .IsRequired();

            snapshot.Property(s => s.SnapshotTakenAt)
                .HasColumnName("TourSnapshot_SnapshotTakenAt")
                .IsRequired();
        });
    }
}
