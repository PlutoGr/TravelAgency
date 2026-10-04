using FluentValidation;
using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.Features.Tours.Manage;

public sealed class CreateTourDraftCommandValidator : AbstractValidator<CreateTourDraftCommand>
{
    public CreateTourDraftCommandValidator()
    {
        RuleFor(x => x.Request!.Title).MaximumLength(200).When(x => x.Request?.Title is not null);
        RuleFor(x => x.Request!.ShortDescription)
            .MaximumLength(TourContentLimits.ShortDescriptionMaxLength)
            .When(x => x.Request?.ShortDescription is not null);
        RuleFor(x => x.Request!.Description).MaximumLength(2000).When(x => x.Request?.Description is not null);
        RuleFor(x => x.Request!.DepartureCity).MaximumLength(100).When(x => x.Request?.DepartureCity is not null);
        RuleFor(x => x.Request!.Country).MaximumLength(100).When(x => x.Request?.Country is not null);
        RuleFor(x => x.Request!.DurationDays).GreaterThanOrEqualTo(0).When(x => x.Request?.DurationDays is not null);
        RuleFor(x => x.Request!.TourType).Must(BeTourType).When(x => !string.IsNullOrWhiteSpace(x.Request?.TourType));
    }

    private static bool BeTourType(string? value) =>
        Enum.TryParse<TourType>(value, ignoreCase: true, out _);
}

public sealed class UpdateTourBasicsCommandValidator : AbstractValidator<UpdateTourBasicsCommand>
{
    public UpdateTourBasicsCommandValidator()
    {
        RuleFor(x => x.Request.Title).MaximumLength(200);
        RuleFor(x => x.Request.ShortDescription).MaximumLength(TourContentLimits.ShortDescriptionMaxLength);
        RuleFor(x => x.Request.DepartureCity).MaximumLength(100);
        RuleFor(x => x.Request.Country).MaximumLength(100);
        RuleFor(x => x.Request.DurationDays).GreaterThanOrEqualTo(0).When(x => x.Request.DurationDays is not null);
        RuleFor(x => x.Request.TourType)
            .Must(value => string.IsNullOrWhiteSpace(value) || Enum.TryParse<TourType>(value, true, out _))
            .WithMessage("Tour type is invalid.");
    }
}

public sealed class UpdateTourDescriptionCommandValidator : AbstractValidator<UpdateTourDescriptionCommand>
{
    public UpdateTourDescriptionCommandValidator()
    {
        RuleFor(x => x.Request.Description).MaximumLength(2000);
    }
}

public sealed class UpdateTourProgramCommandValidator : AbstractValidator<UpdateTourProgramCommand>
{
    public UpdateTourProgramCommandValidator()
    {
        RuleFor(x => x.Request.Days).NotNull();
        RuleForEach(x => x.Request.Days).ChildRules(day =>
        {
            day.RuleFor(d => d.DayNumber).GreaterThanOrEqualTo(1);
            day.RuleFor(d => d.Title).NotEmpty().MaximumLength(200);
            day.RuleFor(d => d.Description).NotEmpty().MaximumLength(4000);
        });
        RuleFor(x => x.Request.Days)
            .Must(days => days is null || days.Select(d => d.DayNumber).Distinct().Count() == days.Count)
            .WithMessage("Day numbers must be unique.");
    }
}

public sealed class UpdateTourConditionsCommandValidator : AbstractValidator<UpdateTourConditionsCommand>
{
    public UpdateTourConditionsCommandValidator()
    {
        RuleFor(x => x.Request.AccommodationText).MaximumLength(4000);
        RuleFor(x => x.Request.MealPlan)
            .Must(value => string.IsNullOrWhiteSpace(value) || Enum.TryParse<MealPlan>(value, true, out _))
            .WithMessage("Meal plan is invalid.");
        RuleForEach(x => x.Request.Inclusions).ChildRules(item =>
        {
            item.RuleFor(i => i.Text).NotEmpty().MaximumLength(500);
            item.RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
            item.RuleFor(i => i.Kind)
                .Must(value => Enum.TryParse<TourInclusionKind>(value, true, out _))
                .WithMessage("Inclusion kind is invalid.");
        });
    }
}

public sealed class UpdateTourManagePricesCommandValidator : AbstractValidator<UpdateTourManagePricesCommand>
{
    public UpdateTourManagePricesCommandValidator()
    {
        RuleFor(x => x.Request.Offers).NotNull();
        RuleForEach(x => x.Request.Offers).ChildRules(offer =>
        {
            offer.RuleFor(o => o.PricePerPerson).GreaterThan(0);
            offer.RuleFor(o => o.AvailableSeats).GreaterThanOrEqualTo(0);
            offer.RuleFor(o => o.Currency)
                .NotEmpty()
                .Length(3)
                .Must(currency => currency!.All(char.IsLetter));
            offer.RuleFor(o => o)
                .Must(o => o.ValidFrom < o.ValidTo)
                .WithMessage("ValidFrom must be earlier than ValidTo.");
        });
    }
}

public sealed class UpdateTourImagesCommandValidator : AbstractValidator<UpdateTourImagesCommand>
{
    public UpdateTourImagesCommandValidator()
    {
        RuleFor(x => x.Request.Images).NotNull();
        RuleFor(x => x.Request.Images)
            .Must(images => images is null || images.Count <= TourContentLimits.MaxImagesPerTour)
            .WithMessage($"A tour cannot have more than {TourContentLimits.MaxImagesPerTour} images.");
        RuleFor(x => x.Request.Images)
            .Must(images => images is null || images.Count(i => i.IsCover) <= 1)
            .WithMessage("A tour can have only one cover image.");
        RuleFor(x => x.Request.Images)
            .Must(images => images is null || images.Select(i => i.MediaFileId).Distinct().Count() == images.Count)
            .WithMessage("Media file ids must be unique.");
        RuleForEach(x => x.Request.Images).ChildRules(image =>
        {
            image.RuleFor(i => i.MediaFileId).NotEmpty();
            image.RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
            image.RuleFor(i => i.Alt).MaximumLength(300);
        });
    }
}
