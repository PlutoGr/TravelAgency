using Microsoft.EntityFrameworkCore;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Infrastructure.Persistence;

public class CatalogDbContext : DbContext, IUnitOfWork
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<Direction> Directions => Set<Direction>();
    public DbSet<TourPrice> TourPrices => Set<TourPrice>();

    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
    }
}
