namespace TravelAgency.Catalog.Domain;

/// <summary>
/// Единственный источник кодов проверки публикации.
/// Тексты для пользователя живут на фронте; здесь только стабильные коды.
/// </summary>
public static class TourPublishRequirementCodes
{
    public const string Title = "title";
    public const string ShortDescription = "shortDescription";
    public const string Description = "description";
    public const string ProgramDayCount = "program.dayCount";
    public const string InclusionsIncluded = "inclusions.included";
    public const string MealPlan = "mealPlan";
    public const string Accommodation = "accommodation";
    public const string OffersFuture = "offers.future";
    public const string ImagesCover = "images.cover";
    public const string ImagesMinCount = "images.minCount";

    /// <summary>
    /// Ширина оригинала обложки меньше 1280 px.
    /// Само значение ширины приходит на вход проверки (из метаданных снимка или аргументом от #37).
    /// Catalog не вызывает Media.
    /// </summary>
    public const string ImagesCoverMinWidth = "images.coverMinWidth";

    public static IReadOnlyList<string> All { get; } =
    [
        Title,
        ShortDescription,
        Description,
        ProgramDayCount,
        InclusionsIncluded,
        MealPlan,
        Accommodation,
        OffersFuture,
        ImagesCover,
        ImagesMinCount,
        ImagesCoverMinWidth
    ];
}
