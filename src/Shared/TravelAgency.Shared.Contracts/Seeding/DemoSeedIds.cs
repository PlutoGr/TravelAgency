namespace TravelAgency.Shared.Contracts.Seeding;

/// <summary>
/// Фиксированные идентификаторы демо-данных.
/// Identity, Catalog и Media читают одни и те же значения: OwnerId тура
/// совпадает с пользователем, а фото тура — с файлом в Media.
/// </summary>
public static class DemoSeedIds
{
    public const int PublishedTourCount = 5;
    public const int PhotosPerTour = 4;
    public const int PhotoWidthPx = 1600;

    public static readonly Guid ClientId = new("11111111-1111-4111-8111-111111111111");
    public static readonly Guid ManagerId = new("22222222-2222-4222-8222-222222222222");
    public static readonly Guid AdminId = new("33333333-3333-4333-8333-333333333333");
    public static readonly Guid Manager2Id = new("44444444-4444-4444-8444-444444444444");

    public static readonly Guid MaldivesTourId = new("a1000001-0000-4000-8000-000000000001");
    public static readonly Guid PhuketTourId = new("a1000002-0000-4000-8000-000000000002");
    public static readonly Guid SantoriniTourId = new("a1000003-0000-4000-8000-000000000003");
    public static readonly Guid BaliTourId = new("a1000004-0000-4000-8000-000000000004");
    public static readonly Guid DubaiTourId = new("a1000005-0000-4000-8000-000000000005");
    public static readonly Guid Manager2DraftTourId = new("a1000006-0000-4000-8000-000000000006");

    public static readonly IReadOnlyList<Guid> PublishedTourIds =
    [
        MaldivesTourId,
        PhuketTourId,
        SantoriniTourId,
        BaliTourId,
        DubaiTourId
    ];

    public static Guid PhotoId(int tourNumber, int photoNumber)
    {
        if (tourNumber is < 1 or > PublishedTourCount)
            throw new ArgumentOutOfRangeException(nameof(tourNumber));
        if (photoNumber is < 1 or > PhotosPerTour)
            throw new ArgumentOutOfRangeException(nameof(photoNumber));

        return new Guid($"b100000{tourNumber}-0000-4000-8000-00000000000{photoNumber}");
    }
}
