using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Shared.Contracts.Seeding;
using TravelAgency.Shared.Infrastructure.Seeding;

namespace TravelAgency.Catalog.Infrastructure.Seeding;

/// <summary>
/// Дозаполняет 5 демо-туров до модели, которую проходит Publish(), и один черновик второго менеджера.
/// Уже существующий тур ищется по названию и сохраняет свой id. Его предложения не удаляются:
/// добавляется только то, чего не хватает для публикации. Фиксированный id — у новой строки.
/// Повторный запуск не создаёт второй ряд. Колонку ImageUrl не удаляет.
/// </summary>
public sealed class CatalogDataSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<CatalogDataSeeder> logger) : IHostedService
{
    /// <summary>Флаг демо-каталога вне Development: Seeding__DemoCatalog=true.</summary>
    public const string DemoCatalogKey = DemoSeedGate.DemoCatalogKey;

    public const string Manager2DraftTitle = "Личный черновик второго менеджера";

    public static bool ShouldSeed(IHostEnvironment environment, IConfiguration configuration) =>
        DemoSeedGate.ShouldSeed(environment, configuration, includeDemoCatalogFlag: true);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!ShouldSeed(environment, configuration))
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            await SeedAsync(db, logger, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Catalog seed failed (non-fatal)");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static async Task SeedAsync(CatalogDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        var directions = await EnsureDirectionsAsync(db, cancellationToken);
        var now = DateTime.UtcNow;
        var tours = await db.Tours
            .Include(t => t.Offers)
            .Include(t => t.Days)
            .Include(t => t.Inclusions)
            .Include(t => t.Images)
            .ToListAsync(cancellationToken);

        foreach (var spec in DemoTours)
        {
            directions.TryGetValue(spec.DirectionName, out var direction);
            var tour = tours.FirstOrDefault(t => t.Title == spec.Title)
                ?? tours.FirstOrDefault(t => t.Id == spec.Id);
            if (tour is null)
            {
                tour = Tour.Create(
                    spec.Title,
                    spec.Description,
                    spec.Type,
                    spec.Country,
                    spec.Days,
                    spec.ImageUrl,
                    direction?.Id,
                    DemoSeedIds.ManagerId,
                    spec.Id);
                db.Tours.Add(tour);
                tours.Add(tour);
            }
            else if (IsReady(tour, spec.Number, now))
            {
                continue;
            }
            else if (tour.Status == TourStatus.Published)
            {
                tour.Unpublish();
            }

            if (!HasExactProgram(tour, spec.Days) && tour.Days.Count > 0)
            {
                tour.ReplaceDays([]);
                await db.SaveChangesAsync(cancellationToken);
            }

            tour.SetBasics(
                spec.Title,
                spec.ShortDescription,
                spec.DepartureCity,
                spec.Country,
                spec.Type,
                spec.Days,
                direction?.Id);
            tour.SetDescription(spec.Description);
            if (!HasExactProgram(tour, spec.Days))
                tour.ReplaceDays(Days(tour.Id, spec.Days, spec.Title));

            if (tour.Inclusions.All(i => i.Kind != TourInclusionKind.Included)
                || tour.MealPlan is null
                || string.IsNullOrWhiteSpace(tour.AccommodationText))
            {
                tour.ReplaceConditions(
                    [
                        TourInclusion.Create(tour.Id, "Проживание и питание по программе", TourInclusionKind.Included, 0),
                        TourInclusion.Create(tour.Id, "Трансфер аэропорт — отель — аэропорт", TourInclusionKind.Included, 1),
                        TourInclusion.Create(tour.Id, "Личные расходы и экскурсии вне программы", TourInclusionKind.NotIncluded, 2)
                    ],
                    spec.Meal,
                    spec.Stay);
            }

            if (!tour.Offers.Any(o => o.ValidFrom <= now && o.ValidTo >= now))
            {
                tour.AddOffer(TourOffer.Create(
                    tour.Id, now.AddDays(-14), now.AddDays(90), spec.Price, "RUB", spec.Seats));
            }

            if (!tour.Offers.Any(o => o.ValidFrom > now))
            {
                var futureFrom = now.AddDays(21);
                tour.AddOffer(TourOffer.Create(
                    tour.Id, futureFrom, futureFrom.AddDays(spec.Days), spec.Price, "RUB", spec.Seats));
            }

            AddMissingImages(tour, spec.Number);
            tour.Publish(now, DemoSeedIds.PhotoWidthPx);
        }

        await EnsureManager2DraftAsync(db, tours, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded demo catalog");
    }

    private static bool IsReady(Tour tour, int tourNumber, DateTime utcNow)
    {
        if (tour.Status != TourStatus.Published)
            return false;

        for (var photo = 1; photo <= DemoSeedIds.PhotosPerTour; photo++)
        {
            var mediaId = DemoSeedIds.PhotoId(tourNumber, photo);
            if (tour.Images.All(image => image.MediaFileId != mediaId))
                return false;
        }

        return tour.GetMissingPublishRequirements(utcNow).Count == 0;
    }

    private static bool HasExactProgram(Tour tour, int durationDays)
    {
        if (tour.Days.Count != durationDays)
            return false;

        for (var day = 1; day <= durationDays; day++)
        {
            if (tour.Days.All(d => d.DayNumber != day))
                return false;
        }

        return true;
    }

    private static void AddMissingImages(Tour tour, int tourNumber)
    {
        var hasCover = tour.Images.Any(image => image.IsCover);
        for (var photo = 1; photo <= DemoSeedIds.PhotosPerTour; photo++)
        {
            var mediaId = DemoSeedIds.PhotoId(tourNumber, photo);
            if (tour.Images.Any(image => image.MediaFileId == mediaId))
                continue;

            var isCover = !hasCover && photo == 1;
            tour.AddImage(TourImage.Create(
                tour.Id,
                mediaId,
                photo - 1,
                isCover,
                $"Фото {photo}",
                DemoSeedIds.PhotoWidthPx));
            if (isCover)
                hasCover = true;
        }
    }

    private static async Task EnsureManager2DraftAsync(
        CatalogDbContext db,
        List<Tour> tours,
        CancellationToken cancellationToken)
    {
        var draft = tours.FirstOrDefault(t => t.Id == DemoSeedIds.Manager2DraftTourId)
            ?? tours.FirstOrDefault(t => t.Title == Manager2DraftTitle);
        if (draft is not null)
            return;

        draft = Tour.CreateDraft(DemoSeedIds.Manager2Id, DemoSeedIds.Manager2DraftTourId);
        draft.SetTitle(Manager2DraftTitle);
        db.Tours.Add(draft);
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static IEnumerable<TourDay> Days(Guid tourId, int count, string title)
    {
        for (var day = 1; day <= count; day++)
            yield return TourDay.Create(tourId, day, $"День {day}", $"{title}: программа дня {day}.");
    }

    private static async Task<Dictionary<string, Direction>> EnsureDirectionsAsync(
        CatalogDbContext db,
        CancellationToken cancellationToken)
    {
        var existing = await db.Directions.ToListAsync(cancellationToken);
        var byName = existing.ToDictionary(d => d.Name, StringComparer.Ordinal);

        foreach (var (name, country, description) in Directions)
        {
            if (byName.ContainsKey(name))
                continue;

            var direction = Direction.Create(name, country, description);
            db.Directions.Add(direction);
            byName[name] = direction;
        }

        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync(cancellationToken);

        return byName;
    }

    private static readonly (string Name, string Country, string Description)[] Directions =
    [
        ("Мальдивы", "Мальдивы", "Райские острова с лазурной водой и белоснежными пляжами"),
        ("Пхукет", "Таиланд", "Тропический рай с храмами, пляжами и экзотической кухней"),
        ("Санторини", "Греция", "Белые дома на скалах с видом на Эгейское море"),
        ("Бали", "Индонезия", "Остров богов с рисовыми террасами и храмами"),
        ("Дубай", "ОАЭ", "Современный мегаполис с небоскрёбами и пустыней"),
    ];

    private sealed record DemoTour(
        int Number,
        Guid Id,
        string Title,
        string ShortDescription,
        string Description,
        TourType Type,
        string Country,
        string DepartureCity,
        int Days,
        string ImageUrl,
        string DirectionName,
        MealPlan Meal,
        string Stay,
        decimal Price,
        int Seats);

    private static readonly DemoTour[] DemoTours =
    [
        new(
            1,
            DemoSeedIds.MaldivesTourId,
            "Мальдивы — рай на земле",
            "Водные виллы, белый песок и снорклинг на атолле.",
            "Уникальные водные виллы над лазурным океаном. Белоснежный песок, кристально чистая вода и богатый подводный мир.",
            TourType.Beach,
            "Мальдивы",
            "Москва",
            7,
            "https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=800",
            "Мальдивы",
            MealPlan.AI,
            "Водная вилла, всё включено",
            289_000,
            8),
        new(
            2,
            DemoSeedIds.PhuketTourId,
            "Экзотический Таиланд — Пхукет",
            "Пляжи Пхукета, острова Пхи-Пхи и тайская кухня.",
            "Тропический рай с белоснежными пляжами и бирюзовым морем. Экскурсии на острова Пхи-Пхи, храмы и тайская кухня.",
            TourType.Beach,
            "Таиланд",
            "Москва",
            11,
            "https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?w=800",
            "Пхукет",
            MealPlan.BB,
            "Отель у пляжа Патонг, завтраки",
            124_000,
            15),
        new(
            3,
            DemoSeedIds.SantoriniTourId,
            "Санторини — романтика Эгейского моря",
            "Белые дома, закат в Ое и греческая кухня.",
            "Белые дома с синими куполами на вулканических скалах. Закаты, вино и греческая кухня.",
            TourType.Cultural,
            "Греция",
            "Москва",
            5,
            "https://images.unsplash.com/photo-1613395877344-13d4a8e0d49e?w=800",
            "Санторини",
            MealPlan.HB,
            "Пещерный отель в Фире, полупансион",
            89_000,
            10),
        new(
            4,
            DemoSeedIds.BaliTourId,
            "Бали — остров богов",
            "Рисовые террасы, храмы и пляжи Семиньяка.",
            "Рисовые террасы Тегаллаланг, храм Танах Лот, пляжи Семиньяк. Йога, спа и индонезийская кухня.",
            TourType.Adventure,
            "Индонезия",
            "Москва",
            10,
            "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=800",
            "Бали",
            MealPlan.BB,
            "Вилла в Убуде, завтраки",
            156_000,
            8),
        new(
            5,
            DemoSeedIds.DubaiTourId,
            "Дубай — роскошь и приключения",
            "Бурдж-Халифа, пустыня и пальмовые острова.",
            "Бурдж-Халифа, пальмовые острова, пустыня и аквапарки. Шопинг и восточная экзотика.",
            TourType.City,
            "ОАЭ",
            "Москва",
            7,
            "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=800",
            "Дубай",
            MealPlan.BB,
            "Отель в Марине, завтраки",
            198_000,
            20),
    ];
}
