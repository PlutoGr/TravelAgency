using FluentAssertions;
using TravelAgency.Booking.Application.Mapping;
using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.UnitTests.Application.Mapping;

public class BookingMapperPhoneTests
{
    [Fact]
    public void ToDto_LeavesClientPhoneEmpty()
    {
        var booking = BookingEntity.Create(Guid.NewGuid(), Guid.NewGuid(), "комментарий");

        var dto = booking.ToDto("Анна Смирнова", "anna@example.com", assignedManagerName: "Марина Иванова");

        dto.ClientPhone.Should().BeNull();
        dto.ClientEmail.Should().Be("anna@example.com");
        dto.ClientName.Should().Be("Анна Смирнова");
    }
}
