using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext, IUnitOfWork
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Direction> Directions => Set<Direction>();
    public DbSet<TourOffer> TourOffers => Set<TourOffer>();
    public DbSet<TourDay> TourDays => Set<TourDay>();
    public DbSet<TourInclusion> TourInclusions => Set<TourInclusion>();
    public DbSet<TourImage> TourImages => Set<TourImage>();
    public DbSet<TourComponent> TourComponents => Set<TourComponent>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);

        // xmin есть только в PostgreSQL. SQLite-тесты собирают модель без этого столбца,
        // иначе EnsureCreated пытается читать системную колонку, которой нет.
        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // Npgsql сопоставляет uint + IsRowVersion с системной колонкой xmin и не добавляет её в миграцию.
            modelBuilder.Entity<Tour>().Property<uint>("xmin").IsRowVersion();
        }
    }
}
