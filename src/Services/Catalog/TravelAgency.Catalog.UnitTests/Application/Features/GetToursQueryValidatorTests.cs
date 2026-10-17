using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

namespace TravelAgency.Catalog.UnitTests.Application.Features;

public class GetToursQueryValidatorTests
{
    private readonly GetToursQueryValidator _validator = new();

    [Fact]
    public void Validate_WhenDateFromIsAfterDateTo_FailsWithDateRangeCode()
    {
        var query = new GetToursQuery(new ToursFilterDto(
            DateFrom: new DateOnly(2026, 12, 1),
            DateTo: new DateOnly(2026, 11, 1)));

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.PropertyName.Should().Be("DateTo");
        error.ErrorCode.Should().Be("date_range");
        error.ErrorMessage.Should().Be("Дата «по» должна быть не раньше даты «с».");
    }

    [Theory]
    [InlineData("2026-11-01", "2026-11-15")]
    [InlineData("2026-11-15", "2026-11-15")]
    public void Validate_WhenRangeIsOrderedOrSingleDay_Passes(string from, string to)
    {
        var query = new GetToursQuery(new ToursFilterDto(
            DateFrom: DateOnly.Parse(from),
            DateTo: DateOnly.Parse(to)));

        _validator.Validate(query).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenOnlyOneBoundIsSet_Passes()
    {
        var fromOnly = new GetToursQuery(new ToursFilterDto(DateFrom: new DateOnly(2026, 11, 1)));
        var toOnly = new GetToursQuery(new ToursFilterDto(DateTo: new DateOnly(2026, 11, 15)));

        _validator.Validate(fromOnly).IsValid.Should().BeTrue();
        _validator.Validate(toOnly).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("1999-12-31", null, "DateFrom")]
    [InlineData(null, "1999-12-31", "DateTo")]
    [InlineData("9999-12-31", null, "DateFrom")]
    [InlineData(null, "9999-12-31", "DateTo")]
    [InlineData("2101-01-01", "2026-11-01", "DateFrom")]
    [InlineData("2026-11-01", "1999-12-31", "DateTo")]
    public void Validate_WhenDateIsOutsideSupportedYears_FailsWithDateRangeCode(
        string? from,
        string? to,
        string propertyName)
    {
        var query = new GetToursQuery(new ToursFilterDto(
            DateFrom: from is null ? null : DateOnly.Parse(from),
            DateTo: to is null ? null : DateOnly.Parse(to)));

        var result = _validator.Validate(query);

        result.IsValid.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle().Subject;
        error.PropertyName.Should().Be(propertyName);
        error.ErrorCode.Should().Be("date_range");
        error.ErrorMessage.Should().Be("Выберите дату в пределах 2000\u20132100 года");
        error.ErrorMessage.Should().NotContain("\u2014");
    }

    [Theory]
    [InlineData("2000-01-01", null)]
    [InlineData(null, "2100-12-31")]
    [InlineData("2000-01-01", "2100-12-31")]
    public void Validate_WhenDateIsOnSupportedBoundary_Passes(string? from, string? to)
    {
        var query = new GetToursQuery(new ToursFilterDto(
            DateFrom: from is null ? null : DateOnly.Parse(from),
            DateTo: to is null ? null : DateOnly.Parse(to)));

        _validator.Validate(query).IsValid.Should().BeTrue();
    }
}
