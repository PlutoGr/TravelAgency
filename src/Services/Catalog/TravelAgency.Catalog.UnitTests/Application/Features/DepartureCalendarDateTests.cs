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

    [Fact]
    public void UtcBounds_AreInclusiveForTheWholeDepartureDay()
    {
        var day = new DateOnly(2026, 11, 15);
        var start = DepartureCalendarDate.UtcStart(day);
        var end = DepartureCalendarDate.UtcEndExclusive(day);

        start.Kind.Should().Be(DateTimeKind.Utc);
        end.Kind.Should().Be(DateTimeKind.Utc);
        start.Should().Be(new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc));
        end.Should().Be(new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc));

        var evening = new DateTime(2026, 11, 15, 21, 15, 0, DateTimeKind.Utc);
        var nextMidnight = new DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Utc);
        (evening >= start && evening < end).Should().BeTrue();
        (nextMidnight >= start && nextMidnight < end).Should().BeFalse();
    }
}
