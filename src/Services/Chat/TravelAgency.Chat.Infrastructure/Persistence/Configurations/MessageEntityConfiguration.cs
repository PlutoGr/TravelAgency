using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using TravelAgency.Chat.Domain.Entities;
using TravelAgency.Chat.Domain.Enums;

namespace TravelAgency.Chat.Infrastructure.Persistence.Configurations;

public class MessageEntityConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.ToTable("chat_messages");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id");

        builder.Property(e => e.BookingId)
            .HasColumnName("booking_id")
            .IsRequired();

        builder.Property(e => e.SenderId)
            .HasColumnName("sender_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.SenderName)
            .HasColumnName("sender_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.SenderRole)
            .HasColumnName("sender_role")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Text)
            .HasColumnName("text")
            .IsRequired();

        builder.Property(e => e.Attachments)
            .HasColumnName("attachments")
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v.ToList()),
                v => string.IsNullOrEmpty(v) ? null : (IReadOnlyList<string>)(JsonSerializer.Deserialize<List<string>>(v) ?? new List<string>()).AsReadOnly());

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(e => e.BookingId)
            .HasDatabaseName("ix_chat_messages_booking_id");
    }
}
