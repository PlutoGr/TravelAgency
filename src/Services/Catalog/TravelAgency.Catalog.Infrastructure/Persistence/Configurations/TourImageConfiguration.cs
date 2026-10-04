using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Infrastructure.Persistence.Configurations;

public class TourImageConfiguration : IEntityTypeConfiguration<TourImage>
{
    public const string OneCoverIndexName = "IX_TourImages_OneCoverPerTour";

    public void Configure(EntityTypeBuilder<TourImage> builder)
    {
        builder.ToTable("TourImages");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Alt)
            .HasMaxLength(300);

        builder.HasIndex(i => new { i.TourId, i.SortOrder })
            .HasDatabaseName("IX_TourImages_TourId_SortOrder");

        // Одна обложка на тур. Строки с IsCover = false в индекс не попадают.
        // Фильтр без сравнения с TRUE/1: и PostgreSQL (boolean), и SQLite (0/1) считают его истинным.
        builder.HasIndex(i => i.TourId)
            .IsUnique()
            .HasFilter("\"IsCover\"")
            .HasDatabaseName(OneCoverIndexName);
    }
}
