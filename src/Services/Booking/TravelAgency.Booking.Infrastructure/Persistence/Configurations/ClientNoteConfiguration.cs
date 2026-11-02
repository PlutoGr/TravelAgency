using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Booking.Domain.Entities;

namespace TravelAgency.Booking.Infrastructure.Persistence.Configurations;

public class ClientNoteConfiguration : IEntityTypeConfiguration<ClientNote>
{
    public void Configure(EntityTypeBuilder<ClientNote> builder)
    {
        builder.ToTable("ClientNotes");
        builder.HasKey(note => note.Id);
        builder.Property(note => note.Id).ValueGeneratedNever();
        builder.Property(note => note.ClientId).IsRequired();
        builder.Property(note => note.AuthorId).IsRequired();
        builder.Property(note => note.Text).IsRequired().HasMaxLength(ClientNote.MaxTextLength);
        builder.Property(note => note.CreatedAt).IsRequired();
        builder.HasIndex(note => note.ClientId);
    }
}
