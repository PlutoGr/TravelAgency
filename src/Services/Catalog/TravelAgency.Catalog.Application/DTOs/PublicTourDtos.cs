using System.Text.Json.Serialization;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.DTOs;

/// <summary>Превью фото. Url — публичный путь Gateway на размер w800.</summary>
public record PublicTourPreviewDto(
    Guid MediaFileId,
    string Url,
    string? Alt,
    bool IsCover,
    int SortOrder);

public record PublicTourDayDto(int DayNumber, string Title, string Description);

public record PublicTourInclusionDto(string Text, string Kind, int SortOrder);

/// <summary>Задел этапа 2. На этапе 1 список на странице тура всегда пустой.</summary>
public record PublicTourComponentDto(Guid Id, string Name);

/// <summary>
/// Карточка для избранного. У снятого тура заполнены только id, название, обложка и available=false.
/// </summary>
public record PublicTourCardDto(
    Guid Id,
    string Title,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    PublicTourPreviewDto? Cover,
    bool Available,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    decimal? PriceFrom,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Currency,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    DateTime? NearestDate,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? ShortDescription,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Country,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    int? DurationDays);

/// <summary>Страница опубликованного тура. Черновик и снятый сюда не попадают.</summary>
public record PublicTourDto(
    Guid Id,
    string Title,
    string? ShortDescription,
    string Description,
    string? DepartureCity,
    string Country,
    TourType TourType,
    int DurationDays,
    string? ImageUrl,
    Guid? DirectionId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? PublishedAt,
    string? MealPlan,
    string? AccommodationText,
    decimal? PriceFrom,
    string? Currency,
    DateTime? NearestDate,
    IReadOnlyList<TourPriceDto> Prices,
    IReadOnlyList<PublicTourDayDto> Days,
    IReadOnlyList<PublicTourInclusionDto> Inclusions,
    IReadOnlyList<PublicTourPreviewDto> Images,
    IReadOnlyList<PublicTourComponentDto> Components);
