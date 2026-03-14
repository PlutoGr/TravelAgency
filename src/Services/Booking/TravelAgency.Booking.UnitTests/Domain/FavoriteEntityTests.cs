using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Exceptions;

namespace TravelAgency.Booking.UnitTests.Domain;

public class FavoriteEntityTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateFavorite()
    {
        var userId = Guid.NewGuid();
        var tourId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var favorite = Favorite.Create(userId, tourId);
        var after = DateTime.UtcNow;

        favorite.Id.Should().NotBe(Guid.Empty);
        favorite.UserId.Should().Be(userId);
        favorite.TourId.Should().Be(tourId);
        favorite.AddedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldThrow()
    {
        var act = () => Favorite.Create(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*userId*");
    }

    [Fact]
    public void Create_WithEmptyTourId_ShouldThrow()
    {
        var act = () => Favorite.Create(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<BookingDomainException>()
            .WithMessage("*tourId*");
    }
}
