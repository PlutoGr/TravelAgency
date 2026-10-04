using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.DTOs;

public record CreateTourDraftRequest(
    string? Title,
    string? ShortDescription,
    string? Description,
    string? DepartureCity,
    string? Country,
    string? TourType,
    int? DurationDays,
    Guid? DirectionId);

public record UpdateTourBasicsRequest(
    string? Title,
    string? ShortDescription,
    string? DepartureCity,
    string? Country,
    string? TourType,
    int? DurationDays,
    Guid? DirectionId);

public record UpdateTourDescriptionRequest(string? Description);

public record UpdateTourProgramRequest(IReadOnlyList<TourDayInput>? Days);

public record TourDayInput(int DayNumber, string? Title, string? Description);

public record UpdateTourConditionsRequest(
    IReadOnlyList<TourInclusionInput>? Inclusions,
    string? MealPlan,
    string? AccommodationText);

public record TourInclusionInput(string? Text, string? Kind, int SortOrder);

public record UpdateTourManagePricesRequest(IReadOnlyList<TourOfferInput>? Offers);

public record TourOfferInput(
    DateTime ValidFrom,
    DateTime ValidTo,
    decimal PricePerPerson,
    string? Currency,
    int AvailableSeats);

public record UpdateTourImagesRequest(IReadOnlyList<TourImageInput>? Images);

public record TourImageInput(Guid MediaFileId, int SortOrder, bool IsCover, string? Alt);

public record TourManageDto(
    Guid Id,
    string Title,
    string? ShortDescription,
    string Description,
    string? DepartureCity,
    string Country,
    string TourType,
    int DurationDays,
    Guid? DirectionId,
    string? MealPlan,
    string? AccommodationText,
    string Status,
    string Source,
    Guid? OwnerId,
    long Version,
    string Etag,
    IReadOnlyList<TourDayItemDto> Days,
    IReadOnlyList<TourInclusionItemDto> Inclusions,
    IReadOnlyList<TourOfferItemDto> Offers,
    IReadOnlyList<TourImageItemDto> Images);

public record TourDayItemDto(Guid Id, int DayNumber, string Title, string Description);

public record TourInclusionItemDto(Guid Id, string Text, string Kind, int SortOrder);

public record TourOfferItemDto(
    Guid Id,
    DateTime ValidFrom,
    DateTime ValidTo,
    decimal PricePerPerson,
    string Currency,
    int AvailableSeats);

public record TourImageItemDto(Guid Id, Guid MediaFileId, int SortOrder, bool IsCover, string? Alt, int? WidthPx);
