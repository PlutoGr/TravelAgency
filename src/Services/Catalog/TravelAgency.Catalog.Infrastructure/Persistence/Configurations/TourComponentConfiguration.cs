using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Infrastructure.Persistence.Configurations;

public class TourComponentConfiguration : IEntityTypeConfiguration<TourComponent>
{
    public void Configure(EntityTypeBuilder<TourComponent> builder)
    {
        builder.ToTable("TourComponents");

        builder.HasKey(c => c.Id);
        // Id ставит домен (Guid.NewGuid()). ValueGeneratedOnAdd заставляет EF считать
        // новый компонент уже существующей строкой и делать UPDATE вместо INSERT.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => c.TourId);

        builder.HasOne<Tour>()
            .WithMany()
            .HasForeignKey(c => c.TourId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
