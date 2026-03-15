using FluentValidation;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

public class GetToursQueryValidator : AbstractValidator<GetToursQuery>
{
    public GetToursQueryValidator()
    {
        RuleFor(x => x.Filter.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.Filter.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");
    }
}
