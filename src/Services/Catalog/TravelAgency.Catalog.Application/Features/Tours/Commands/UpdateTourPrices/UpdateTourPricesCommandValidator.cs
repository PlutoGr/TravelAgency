using FluentValidation;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTourPrices;

public class UpdateTourPricesCommandValidator : AbstractValidator<UpdateTourPricesCommand>
{
    public UpdateTourPricesCommandValidator()
    {
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.Request.Prices).NotNull().NotEmpty();

        RuleForEach(x => x.Request.Prices).ChildRules(price =>
        {
            price.RuleFor(p => p.ValidFrom).LessThan(p => p.ValidTo)
                .WithMessage("ValidFrom must be earlier than ValidTo.");
            price.RuleFor(p => p.PricePerPerson).GreaterThan(0);
            price.RuleFor(p => p.AvailableSeats).GreaterThanOrEqualTo(0);
        });
    }
}
