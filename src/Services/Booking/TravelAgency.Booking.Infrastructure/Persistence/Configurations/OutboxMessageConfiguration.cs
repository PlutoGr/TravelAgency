using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Enums;

namespace TravelAgency.Booking.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);
        // Id ставит домен (Guid.NewGuid()), база его не генерирует.
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.EventType).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).IsRequired();
        builder.Property(m => m.Status).IsRequired().HasConversion<int>();
        builder.Property(m => m.CreatedAt).IsRequired();
        builder.Property(m => m.ProcessedAt);

        builder.HasIndex(m => new { m.Status, m.CreatedAt });
    }
}
