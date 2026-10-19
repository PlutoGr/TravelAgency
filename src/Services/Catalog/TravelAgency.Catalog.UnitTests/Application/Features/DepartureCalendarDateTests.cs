using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class DepartureCalendarDateTests
{
    [Theory]
    [InlineData("2026-11-01")]
    [InlineData("2026-11-01T00:00:00Z")]
    [InlineData("2026-11-01T12:00:00Z")]
    [InlineData("2026-11-01T18:00:00+03:00")]
    [InlineData("2026-11-01T00:00:00.0000000Z")]
    public void TryParse_ReadsTheCalendarDayAsWritten(string text)
    {
        DepartureCalendarDate.TryParse(text, out var date).Should().BeTrue();
        date.Should().Be(new DateOnly(2026, 11, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("01.11.2026")]
    [InlineData("2026-11-01foo")]
    public void TryParse_RejectsNonIsoDay(string text)
    {
        DepartureCalendarDate.TryParse(text, out _).Should().BeFalse();
    }

}
