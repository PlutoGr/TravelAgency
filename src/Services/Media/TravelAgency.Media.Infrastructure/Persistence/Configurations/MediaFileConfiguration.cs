using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;

namespace TravelAgency.Media.Infrastructure.Persistence.Configurations;

public sealed class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("MediaFiles");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.OriginalFileName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(f => f.SizeBytes)
            .IsRequired();

        builder.Property(f => f.StorageKey)
            .IsRequired()
            .HasMaxLength(1024);

        builder.Property(f => f.OwnerId)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(f => f.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(f => f.UploadedAt)
            .IsRequired();

        builder.Property(f => f.IsPublic)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(f => f.Width);

        builder.Property(f => f.Height);

        builder.Property(f => f.Purpose)
            .IsRequired()
            .HasMaxLength(32)
            .HasDefaultValue(MediaPurposes.General);

        builder.Navigation(f => f.Thumbnails).HasField("_thumbnails");

        builder.OwnsMany(f => f.Thumbnails, tb =>
        {
            tb.WithOwner().HasForeignKey("MediaFileId");
            tb.HasKey("MediaFileId", "Id");
            tb.Property(t => t.StorageKey).IsRequired().HasMaxLength(1024);
            tb.Property(t => t.Width).IsRequired();
            tb.Property(t => t.Height).IsRequired();
            tb.Property(t => t.SizeCode).HasMaxLength(16);
            tb.ToTable("MediaFileThumbnails");
        });
    }
}
