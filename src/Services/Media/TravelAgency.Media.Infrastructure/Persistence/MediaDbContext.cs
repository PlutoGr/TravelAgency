using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using TravelAgency.Media.Domain.Entities;

namespace TravelAgency.Media.Infrastructure.Persistence;

public sealed class MediaDbContext : DbContext
{
    public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

    public MediaDbContext(DbContextOptions<MediaDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MediaDbContext).Assembly);

        // SQLite does not support composite PK with autoincrement for owned collections.
        // Override to single-column PK when using SQLite (integration tests).
        if (Database.IsSqlite())
        {
            modelBuilder.Entity<MediaFile>().OwnsMany(f => f.Thumbnails, tb =>
            {
                tb.WithOwner().HasForeignKey("MediaFileId");
                tb.HasKey("Id");
                tb.Property(t => t.StorageKey).IsRequired().HasMaxLength(1024);
                tb.Property(t => t.Width).IsRequired();
                tb.Property(t => t.Height).IsRequired();
                tb.Property(t => t.SizeCode).HasMaxLength(16);
                tb.ToTable("MediaFileThumbnails");
            });
        }
    }
}
