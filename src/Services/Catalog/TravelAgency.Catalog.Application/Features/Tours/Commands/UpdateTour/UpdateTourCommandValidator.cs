using FluentValidation;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTour;

public class UpdateTourCommandValidator : AbstractValidator<UpdateTourCommand>
{
    public UpdateTourCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Request.Country).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.DurationDays).GreaterThanOrEqualTo(1);
    }
}
