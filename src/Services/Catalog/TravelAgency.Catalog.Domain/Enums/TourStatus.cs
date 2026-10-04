namespace TravelAgency.Catalog.Domain.Enums;

public enum TourStatus
{
    Draft,
    Published,
    Unpublished,

    /// <summary>Зарезервировано под премодерацию. На этапе 1 не используется.</summary>
    PendingReview
}
