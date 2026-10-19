using Microsoft.EntityFrameworkCore;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Domain.Entities;

namespace TravelAgency.Booking.Infrastructure.Persistence;

public class BookingDbContext : DbContext, IUnitOfWork
{
    public DbSet<Domain.Entities.Booking> Bookings => Set<Domain.Entities.Booking>();
    public DbSet<Proposal> Proposals => Set<Proposal>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<BookingStatusHistory> BookingStatusHistories => Set<BookingStatusHistory>();
    public DbSet<BookingAssignmentHistory> BookingAssignmentHistories => Set<BookingAssignmentHistory>();

    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);

        // xmin есть только в PostgreSQL. SQLite-тесты собирают модель без этого столбца,
        // иначе EnsureCreated пытается читать системную колонку, которой нет.
        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // Npgsql сопоставляет uint + IsRowVersion с системной колонкой xmin и не добавляет её в миграцию.
            modelBuilder.Entity<Domain.Entities.Booking>().Property<uint>("xmin").IsRowVersion();
        }
    }
}
