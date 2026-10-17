using FluentValidation;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

public class GetToursQueryValidator : AbstractValidator<GetToursQuery>
{
    public const string DepartureYearMessage = "Выберите дату в пределах 2000\u20132100 года";

    private static readonly DateOnly MinDeparture = new(2000, 1, 1);
    private static readonly DateOnly MaxDeparture = new(2100, 12, 31);

    public GetToursQueryValidator()
    {
        RuleFor(x => x.Filter.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.Filter.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Filter.DateFrom)
            .Must(date => date is null || IsSupportedDeparture(date.Value))
            .OverridePropertyName("DateFrom")
            .WithErrorCode("date_range")
            .WithMessage(DepartureYearMessage);

        RuleFor(x => x.Filter.DateTo)
            .Must(date => date is null || IsSupportedDeparture(date.Value))
            .OverridePropertyName("DateTo")
            .WithErrorCode("date_range")
            .WithMessage(DepartureYearMessage);

        RuleFor(x => x.Filter)
            .Must(filter => filter.DateFrom!.Value <= filter.DateTo!.Value)
            .When(x => x.Filter.DateFrom is not null && x.Filter.DateTo is not null
                && IsSupportedDeparture(x.Filter.DateFrom.Value)
                && IsSupportedDeparture(x.Filter.DateTo.Value))
            .OverridePropertyName("DateTo")
            .WithErrorCode("date_range")
            .WithMessage("Дата «по» должна быть не раньше даты «с».");
    }

    private static bool IsSupportedDeparture(DateOnly date) =>
        date >= MinDeparture && date <= MaxDeparture;
}
