using FluentValidation;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTourPrices;

public class UpdateTourPricesCommandValidator : AbstractValidator<UpdateTourPricesCommand>
{
    public UpdateTourPricesCommandValidator()
    {
        RuleFor(x => x.TourId).NotEmpty();
        RuleFor(x => x.Request.Prices)
            .Must(p => p != null && p.Count > 0)
            .WithMessage("Prices must not be null or empty.");

        RuleForEach(x => x.Request.Prices).ChildRules(price =>
        {
            price.RuleFor(p => p.ValidFrom).LessThan(p => p.ValidTo)
                .WithMessage("ValidFrom must be earlier than ValidTo.");
            price.RuleFor(p => p.PricePerPerson).GreaterThan(0);
            price.RuleFor(p => p.AvailableSeats).GreaterThanOrEqualTo(0);
            price.RuleFor(p => p.Currency)
                .NotEmpty()
                .Must(c => c != null && c.Trim().Length == 3 && c.Trim().All(char.IsLetter))
                .WithMessage("Currency must be a valid ISO 4217 code (3 letters).");
        });
    }
}
