using FluentValidation;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.CreateTour;

public class CreateTourCommandValidator : AbstractValidator<CreateTourCommand>
{
    public CreateTourCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Request.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.DurationDays).GreaterThanOrEqualTo(1);
    }
}
