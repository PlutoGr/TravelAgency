using FluentValidation;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UpdateManagerCard;

public sealed class UpdateManagerCardCommandValidator : AbstractValidator<UpdateManagerCardCommand>
{
    public UpdateManagerCardCommandValidator()
    {
        RuleFor(x => x.Request.Position)
            .Must(value => ManagerCardText.NormalizeOptional(value) is not { Length: > User.MaxPositionLength })
            .WithMessage("Должность не длиннее 60 символов.")
            .OverridePropertyName("position");

        RuleFor(x => x.Request.TourismSince)
            .Must(year => year is null || (year >= User.MinTourismSinceYear && year <= DateTime.UtcNow.Year))
            .WithMessage("Укажите год с 1980 по текущий.")
            .OverridePropertyName("tourismSince");

        RuleFor(x => x.Request.About)
            .Must(value => ManagerCardText.NormalizeOptional(value) is not { Length: > User.MaxAboutLength })
            .WithMessage("Текст «О себе» не длиннее 1000 символов.")
            .OverridePropertyName("about");

        RuleFor(x => x.Request.Tags)
            .NotNull()
            .WithMessage("Укажите тег.")
            .OverridePropertyName("tags");

        RuleFor(x => x.Request.Tags)
            .Must(tags => tags is null || tags.Count <= User.MaxTagCount)
            .WithMessage("Можно не больше 10.")
            .OverridePropertyName("tags");

        RuleFor(x => x.Request.Tags)
            .Must(tags => tags is null || tags.All(tag => ManagerCardText.NormalizeTag(tag).Length <= User.MaxTagLength))
            .WithMessage("Не длиннее 30 символов.")
            .OverridePropertyName("tags");

        RuleFor(x => x.Request.Tags)
            .Must(tags => tags is null || tags.All(tag => ManagerCardText.NormalizeTag(tag).Length > 0))
            .WithMessage("Укажите тег.")
            .OverridePropertyName("tags");

        RuleFor(x => x.Request.Tags)
            .Must(NoDuplicateTags)
            .WithMessage("Такой тег уже есть.")
            .OverridePropertyName("tags");
    }

    private static bool NoDuplicateTags(IReadOnlyList<string>? tags)
    {
        if (tags is null)
            return true;

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            var normalized = ManagerCardText.NormalizeTag(tag);
            if (normalized.Length == 0)
                continue;
            if (!seen.Add(normalized))
                return false;
        }

        return true;
    }
}
