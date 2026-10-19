using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using TravelAgency.Booking.Infrastructure.Persistence;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Infrastructure;

public class BookingXminConfigurationTests
{
    [Fact]
    public void Booking_UsesPostgresXminAsConcurrencyToken()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseNpgsql("Host=localhost;Database=booking_model;Username=user")
            .Options;

        using var db = new BookingDbContext(options);
        var booking = db.Model.FindEntityType(typeof(BookingEntity));
        var xmin = booking!.FindProperty("xmin");

        xmin.Should().NotBeNull();
        xmin!.IsConcurrencyToken.Should().BeTrue();
        xmin.ValueGenerated.Should().Be(ValueGenerated.OnAddOrUpdate);
    }
}
