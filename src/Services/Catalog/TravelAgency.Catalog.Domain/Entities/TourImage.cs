using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.Domain.Entities;

/// <summary>
/// Снимок фото тура. WidthPx — ширина оригинала, если она уже известна.
/// Catalog сам в Media не ходит: значение записывает вызывающий код (#37), когда метаданные есть.
/// </summary>
public class TourImage
{
    public Guid Id { get; private set; }
    public Guid TourId { get; private set; }
    public Guid MediaFileId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsCover { get; private set; }
    public string? Alt { get; private set; }
    public int? WidthPx { get; private set; }

    private TourImage() { }

    public static TourImage Create(
        Guid tourId,
        Guid mediaFileId,
        int sortOrder,
        bool isCover,
        string? alt = null,
        int? widthPx = null)
    {
        if (mediaFileId == Guid.Empty)
            throw new CatalogDomainException("Media file id is required.");

        if (sortOrder < 0)
            throw new CatalogDomainException("Image sort order cannot be negative.");

        if (widthPx is < 1)
            throw new CatalogDomainException("Image width must be positive when it is known.");

        return new TourImage
        {
            Id = Guid.NewGuid(),
            TourId = tourId,
            MediaFileId = mediaFileId,
            SortOrder = sortOrder,
            IsCover = isCover,
            Alt = string.IsNullOrWhiteSpace(alt) ? null : alt.Trim(),
            WidthPx = widthPx
        };
    }
}
