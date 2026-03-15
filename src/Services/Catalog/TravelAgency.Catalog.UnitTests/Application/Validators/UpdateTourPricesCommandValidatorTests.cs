using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTourPrices;

namespace TravelAgency.Catalog.UnitTests.Application.Validators;

public class UpdateTourPricesCommandValidatorTests
{
    private readonly UpdateTourPricesCommandValidator _validator = new();

    private static UpdateTourPricesCommand BuildCommand(
        Guid? tourId = null,
        List<TourPriceRequest>? prices = null)
    {
        var id = tourId ?? Guid.NewGuid();
        var priceList = prices ?? [
            new TourPriceRequest(
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                1500m,
                "USD",
                10)
        ];
        return new UpdateTourPricesCommand(id, new UpdateTourPricesRequest(priceList));
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        var command = BuildCommand();

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyTourId_ShouldHaveError()
    {
        var command = BuildCommand(tourId: Guid.Empty);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("TourId"));
    }

    [Fact]
    public void Validate_WithNullPrices_ShouldHaveError()
    {
        var command = new UpdateTourPricesCommand(Guid.NewGuid(), new UpdateTourPricesRequest(null!));

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Prices"));
    }

    [Fact]
    public void Validate_WithEmptyPrices_ShouldHaveError()
    {
        var command = BuildCommand(prices: []);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Prices"));
    }

    [Fact]
    public void Validate_WithValidFromAfterValidTo_ShouldHaveError()
    {
        var prices = new List<TourPriceRequest>
        {
            new(
                DateTime.UtcNow.AddDays(30),
                DateTime.UtcNow,
                1000m,
                "EUR",
                5)
        };
        var command = BuildCommand(prices: prices);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage != null && (e.ErrorMessage.Contains("ValidFrom") || e.ErrorMessage.Contains("ValidTo")));
    }

    [Fact]
    public void Validate_WithZeroPricePerPerson_ShouldHaveError()
    {
        var prices = new List<TourPriceRequest>
        {
            new(
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                0m,
                "USD",
                10)
        };
        var command = BuildCommand(prices: prices);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("PricePerPerson"));
    }

    [Fact]
    public void Validate_WithInvalidCurrency_ShouldHaveError()
    {
        var prices = new List<TourPriceRequest>
        {
            new(
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                1000m,
                "US", // Only 2 chars
                10)
        };
        var command = BuildCommand(prices: prices);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Currency"));
    }

    [Fact]
    public void Validate_WithEmptyCurrency_ShouldHaveError()
    {
        var prices = new List<TourPriceRequest>
        {
            new(
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                1000m,
                "",
                10)
        };
        var command = BuildCommand(prices: prices);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Currency"));
    }

    [Fact]
    public void Validate_WithValidEurCurrency_ShouldPass()
    {
        var prices = new List<TourPriceRequest>
        {
            new(
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                1000m,
                "EUR",
                10)
        };
        var command = BuildCommand(prices: prices);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }
}
