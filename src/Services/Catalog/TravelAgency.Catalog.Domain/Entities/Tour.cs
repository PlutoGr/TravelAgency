using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.Domain.Entities;

public class Tour
{
    private readonly List<TourOffer> _offers = [];
    private readonly List<TourDay> _days = [];
    private readonly List<TourInclusion> _inclusions = [];
    private readonly List<TourImage> _images = [];

    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string? ShortDescription { get; private set; }
    public string Description { get; private set; } = default!;
    public TourType TourType { get; private set; }
    public Guid? DirectionId { get; private set; }
    public string Country { get; private set; } = default!;
    public string? DepartureCity { get; private set; }
    public int DurationDays { get; private set; }
    public string? ImageUrl { get; private set; }
    public Enums.MealPlan? MealPlan { get; private set; }
    public string? AccommodationText { get; private set; }
    public TourStatus Status { get; private set; }
    public TourSource Source { get; private set; }
    public Guid? OwnerId { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Версия для If-Match. Увеличивается на каждое успешное изменение.
    /// У существующих строк после миграции остаётся 0.
    /// </summary>
    public long Version { get; private set; }

    /// <summary>
    /// Совместимость публичной выдачи: раньше в каталог попадали туры с IsActive=true.
    /// После миграции это статус Published.
    /// </summary>
    public bool IsActive => Status == TourStatus.Published;

    public IReadOnlyCollection<TourOffer> Offers => _offers.AsReadOnly();
    public IReadOnlyCollection<TourDay> Days => _days.AsReadOnly();
    public IReadOnlyCollection<TourInclusion> Inclusions => _inclusions.AsReadOnly();
    public IReadOnlyCollection<TourImage> Images => _images.AsReadOnly();

    private Tour() { }

    public static Tour Create(
        string title,
        string description,
        TourType tourType,
        string country,
        int durationDays,
        string? imageUrl,
        Guid? directionId = null,
        Guid? ownerId = null,
        Guid? id = null)
    {
        ValidateCore(title, description, country, durationDays);

        return new Tour
        {
            Id = id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            Title = title.Trim(),
            Description = description.Trim(),
            TourType = tourType,
            DirectionId = directionId,
            Country = country.Trim(),
            DurationDays = durationDays,
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            Status = TourStatus.Draft,
            Source = TourSource.Manager,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Пустой черновик мастера. Источник этапа 1 всегда менеджер, владелец — текущий пользователь.
    /// </summary>
    public static Tour CreateDraft(Guid ownerId, Guid? id = null)
    {
        if (ownerId == Guid.Empty)
            throw new CatalogDomainException("Draft owner is required.");

        return new Tour
        {
            Id = id is null || id == Guid.Empty ? Guid.NewGuid() : id.Value,
            Title = string.Empty,
            Description = string.Empty,
            Country = string.Empty,
            DurationDays = 0,
            TourType = TourType.Beach,
            Status = TourStatus.Draft,
            Source = TourSource.Manager,
            OwnerId = ownerId,
            Version = 1,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void AssignOwner(Guid ownerId)
    {
        if (ownerId == Guid.Empty)
            throw new CatalogDomainException("Tour owner is required.");

        if (OwnerId == ownerId)
            return;

        var previous = OwnerId;
        OwnerId = ownerId;
        CommitOrRevert(() => OwnerId = previous);
    }

    public void Update(
        string title,
        string description,
        TourType tourType,
        string country,
        int durationDays,
        string? imageUrl,
        Guid? directionId = null)
    {
        ValidateCore(title, description, country, durationDays);

        var previous = CaptureBasics();
        Title = title.Trim();
        Description = description.Trim();
        TourType = tourType;
        DirectionId = directionId;
        Country = country.Trim();
        DurationDays = durationDays;
        ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        CommitOrRevert(() => RestoreBasics(previous));
    }

    public void SetTitle(string? title)
    {
        var previous = Title;
        Title = title?.Trim() ?? string.Empty;
        CommitOrRevert(() => Title = previous);
    }

    public void SetShortDescription(string? shortDescription)
    {
        var normalized = string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim();
        if (normalized is { Length: > TourContentLimits.ShortDescriptionMaxLength })
            throw new CatalogDomainException(
                $"Short description cannot exceed {TourContentLimits.ShortDescriptionMaxLength} characters.");

        var previous = ShortDescription;
        ShortDescription = normalized;
        CommitOrRevert(() => ShortDescription = previous);
    }

    public void SetDescription(string? description)
    {
        var previous = Description;
        Description = description?.Trim() ?? string.Empty;
        CommitOrRevert(() => Description = previous);
    }

    public void SetDepartureCity(string? departureCity)
    {
        var previous = DepartureCity;
        DepartureCity = string.IsNullOrWhiteSpace(departureCity) ? null : departureCity.Trim();
        CommitOrRevert(() => DepartureCity = previous);
    }

    public void SetBasics(
        string? title,
        string? shortDescription,
        string? departureCity,
        string? country,
        TourType tourType,
        int durationDays,
        Guid? directionId)
    {
        var normalizedTitle = title?.Trim() ?? string.Empty;
        var normalizedShort = string.IsNullOrWhiteSpace(shortDescription) ? null : shortDescription.Trim();
        var normalizedCity = string.IsNullOrWhiteSpace(departureCity) ? null : departureCity.Trim();
        var normalizedCountry = country?.Trim() ?? string.Empty;

        if (normalizedTitle.Length > 200)
            throw new CatalogDomainException("Tour title cannot exceed 200 characters.");

        if (normalizedShort is { Length: > TourContentLimits.ShortDescriptionMaxLength })
            throw new CatalogDomainException(
                $"Short description cannot exceed {TourContentLimits.ShortDescriptionMaxLength} characters.");

        if (normalizedCity is { Length: > 100 })
            throw new CatalogDomainException("Departure city cannot exceed 100 characters.");

        if (normalizedCountry.Length > 100)
            throw new CatalogDomainException("Tour country cannot exceed 100 characters.");

        if (durationDays < 0)
            throw new CatalogDomainException("Tour duration cannot be negative.");

        var previous = CaptureBasics();
        var previousShort = ShortDescription;
        var previousCity = DepartureCity;
        Title = normalizedTitle;
        ShortDescription = normalizedShort;
        DepartureCity = normalizedCity;
        Country = normalizedCountry;
        TourType = tourType;
        DurationDays = durationDays;
        DirectionId = directionId;
        CommitOrRevert(() =>
        {
            RestoreBasics(previous);
            ShortDescription = previousShort;
            DepartureCity = previousCity;
        });
    }

    public void SetConditions(Enums.MealPlan? mealPlan, string? accommodationText)
    {
        var previousMeal = MealPlan;
        var previousStay = AccommodationText;
        MealPlan = mealPlan;
        AccommodationText = string.IsNullOrWhiteSpace(accommodationText) ? null : accommodationText.Trim();
        CommitOrRevert(() =>
        {
            MealPlan = previousMeal;
            AccommodationText = previousStay;
        });
    }

    public void ReplaceConditions(IEnumerable<TourInclusion> inclusions, Enums.MealPlan? mealPlan, string? accommodationText)
    {
        var incoming = inclusions.ToList();
        EnsureOwned(incoming.Select(i => i.TourId), "Inclusion");

        var normalizedStay = string.IsNullOrWhiteSpace(accommodationText) ? null : accommodationText.Trim();
        if (normalizedStay is { Length: > 4000 })
            throw new CatalogDomainException("Accommodation text cannot exceed 4000 characters.");

        var previousInclusions = _inclusions.ToList();
        var previousMeal = MealPlan;
        var previousStay = AccommodationText;
        _inclusions.Clear();
        _inclusions.AddRange(incoming);
        MealPlan = mealPlan;
        AccommodationText = normalizedStay;
        CommitOrRevert(() =>
        {
            _inclusions.Clear();
            _inclusions.AddRange(previousInclusions);
            MealPlan = previousMeal;
            AccommodationText = previousStay;
        });
    }

    public void ReplaceDays(IEnumerable<TourDay> days)
    {
        var incoming = days.ToList();
        EnsureOwned(incoming.Select(d => d.TourId), "Day");
        if (incoming.Select(d => d.DayNumber).Distinct().Count() != incoming.Count)
            throw new CatalogDomainException("Day numbers must be unique.");

        Replace(_days, incoming);
    }

    public void ReplaceInclusions(IEnumerable<TourInclusion> inclusions)
    {
        var incoming = inclusions.ToList();
        EnsureOwned(incoming.Select(i => i.TourId), "Inclusion");
        Replace(_inclusions, incoming);
    }

    public void ReplaceOffers(IEnumerable<TourOffer> offers)
    {
        var incoming = offers.ToList();
        EnsureOwned(incoming.Select(o => o.TourId), "Offer");
        Replace(_offers, incoming);
    }

    public void ReplaceImages(IEnumerable<TourImage> images, int? coverWidthPx = null)
    {
        var incoming = images.ToList();
        EnsureOwned(incoming.Select(i => i.TourId), "Image");

        if (incoming.Count > TourContentLimits.MaxImagesPerTour)
            throw new CatalogDomainException(
                $"A tour cannot have more than {TourContentLimits.MaxImagesPerTour} images.");

        if (incoming.Count(i => i.IsCover) > 1)
            throw new CatalogDomainException("A tour can have only one cover image.");

        Replace(_images, incoming, coverWidthPx);
    }

    /// <summary>
    /// Коды недостающего для публикации.
    /// <paramref name="coverWidthPx"/> — ширина оригинала обложки. Если аргумент не передан,
    /// берётся <see cref="TourImage.WidthPx"/> обложки. Оба пустые — ширина неизвестна и код
    /// <see cref="TourPublishRequirementCodes.ImagesCoverMinWidth"/> остаётся в списке.
    /// Явный аргумент важнее сохранённых метаданных: так #37 передаёт ответ Media, не вызывая его отсюда.
    /// Будущее предложение — то, у которого ValidFrom строго позже <paramref name="utcNow"/>.
    /// </summary>
    public IReadOnlyList<string> GetMissingPublishRequirements(DateTime utcNow, int? coverWidthPx = null)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(Title))
            missing.Add(TourPublishRequirementCodes.Title);

        if (string.IsNullOrWhiteSpace(ShortDescription))
            missing.Add(TourPublishRequirementCodes.ShortDescription);

        if (string.IsNullOrWhiteSpace(Description))
            missing.Add(TourPublishRequirementCodes.Description);

        if (!HasExactProgram())
            missing.Add(TourPublishRequirementCodes.ProgramDayCount);

        if (_inclusions.All(i => i.Kind != TourInclusionKind.Included))
            missing.Add(TourPublishRequirementCodes.InclusionsIncluded);

        if (MealPlan is null)
            missing.Add(TourPublishRequirementCodes.MealPlan);

        if (string.IsNullOrWhiteSpace(AccommodationText))
            missing.Add(TourPublishRequirementCodes.Accommodation);

        if (!_offers.Any(o => o.ValidFrom > utcNow))
            missing.Add(TourPublishRequirementCodes.OffersFuture);

        var cover = _images.SingleOrDefault(i => i.IsCover);
        if (cover is null)
            missing.Add(TourPublishRequirementCodes.ImagesCover);

        if (_images.Count < TourContentLimits.MinImagesToPublish)
            missing.Add(TourPublishRequirementCodes.ImagesMinCount);

        var width = coverWidthPx ?? cover?.WidthPx;
        if (width is null || width < TourContentLimits.CoverMinWidthPx)
            missing.Add(TourPublishRequirementCodes.ImagesCoverMinWidth);

        return missing;
    }

    public void Publish(DateTime utcNow, int? coverWidthPx = null)
    {
        if (Status == TourStatus.PendingReview)
            throw new CatalogDomainException("PendingReview is reserved and is not used in stage 1.");

        var missing = GetMissingPublishRequirements(utcNow, coverWidthPx);
        if (missing.Count > 0)
            throw new TourNotPublishableException(missing);

        Status = TourStatus.Published;
        PublishedAt = utcNow;
        Touch(utcNow);
    }

    public void Unpublish()
    {
        if (Status == TourStatus.Unpublished)
            return;

        if (Status != TourStatus.Published)
            throw new CatalogDomainException("Only a published tour can be unpublished.");

        Status = TourStatus.Unpublished;
        Touch(DateTime.UtcNow);
    }

    /// <summary>
    /// Удалять можно только черновик. Метод не стирает строку: это делает приложение после успешного вызова.
    /// </summary>
    public void Delete()
    {
        if (Status != TourStatus.Draft)
            throw new CatalogDomainException("Only a draft tour can be deleted.");
    }

    private bool HasExactProgram()
    {
        if (DurationDays < 1 || _days.Count != DurationDays)
            return false;

        var numbers = _days.Select(d => d.DayNumber).ToHashSet();
        if (numbers.Count != DurationDays)
            return false;

        for (var day = 1; day <= DurationDays; day++)
        {
            if (!numbers.Contains(day))
                return false;
        }

        return true;
    }

    private void GuardIfPublished(int? coverWidthPx)
    {
        if (Status != TourStatus.Published)
            return;

        var missing = GetMissingPublishRequirements(DateTime.UtcNow, coverWidthPx);
        if (missing.Count > 0)
            throw new TourNotPublishableException(missing);
    }

    private void CommitOrRevert(Action revert, int? coverWidthPx = null)
    {
        try
        {
            GuardIfPublished(coverWidthPx);
            Touch(DateTime.UtcNow);
        }
        catch
        {
            revert();
            throw;
        }
    }

    private void Replace<T>(List<T> target, List<T> incoming, int? coverWidthPx = null)
    {
        var previous = target.ToList();
        target.Clear();
        target.AddRange(incoming);
        CommitOrRevert(() =>
        {
            target.Clear();
            target.AddRange(previous);
        }, coverWidthPx);
    }

    private void EnsureOwned(IEnumerable<Guid> tourIds, string itemName)
    {
        if (tourIds.Any(id => id != Id))
            throw new CatalogDomainException($"{itemName} belongs to another tour.");
    }

    private void Touch(DateTime utcNow)
    {
        Version++;
        UpdatedAt = utcNow;
    }

    private (string Title, string Description, TourType TourType, Guid? DirectionId, string Country, int DurationDays, string? ImageUrl) CaptureBasics()
        => (Title, Description, TourType, DirectionId, Country, DurationDays, ImageUrl);

    private void RestoreBasics(
        (string Title, string Description, TourType TourType, Guid? DirectionId, string Country, int DurationDays, string? ImageUrl) previous)
    {
        Title = previous.Title;
        Description = previous.Description;
        TourType = previous.TourType;
        DirectionId = previous.DirectionId;
        Country = previous.Country;
        DurationDays = previous.DurationDays;
        ImageUrl = previous.ImageUrl;
    }

    private static void ValidateCore(string title, string description, string country, int durationDays)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new CatalogDomainException("Tour title cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new CatalogDomainException("Tour description cannot be empty.");

        if (string.IsNullOrWhiteSpace(country))
            throw new CatalogDomainException("Tour country cannot be empty.");

        if (durationDays < 1)
            throw new CatalogDomainException("Tour duration must be at least 1 day.");
    }
}
