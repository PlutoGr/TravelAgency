using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TravelAgency.Booking.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations. Used by dotnet ef commands.
/// </summary>
internal sealed class DesignTimeBookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../TravelAgency.Booking.API"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("BookingDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "ConnectionStrings:BookingDb is required for migrations. Set ConnectionStrings__BookingDb environment variable or add it to appsettings.json. " +
                "Example for local docker compose: Host=localhost;Port=5433;Database=travel_booking;Username=<POSTGRES_USER>;Password=<POSTGRES_PASSWORD>");

        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new BookingDbContext(options);
    }
}
