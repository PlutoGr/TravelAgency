using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.Infrastructure.Seeding;

/// <summary>
/// Seeds directions and tours when the database is empty. Demo catalog only, no users. See ShouldSeed.
/// </summary>
public sealed class CatalogDataSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<CatalogDataSeeder> logger) : IHostedService
{
    /// <summary>Флаг демо-каталога вне Development: Seeding__DemoCatalog=true.</summary>
    public const string DemoCatalogKey = "Seeding:DemoCatalog";

    /// <summary>
    /// Демо-направления и туры: в Development, при Seeding:DemoCatalog=true
    /// или при устаревшем ASPNETCORE_SEED_DATA=true. Пользователей не создаёт.
    /// </summary>
    public static bool ShouldSeed(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment()
        || configuration.GetValue<bool>(DemoCatalogKey)
        || string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_SEED_DATA"), "true", StringComparison.OrdinalIgnoreCase);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!ShouldSeed(environment, configuration))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

            await SeedDirectionsAsync(db, cancellationToken);
            await SeedToursAsync(db, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Catalog seed failed (non-fatal)");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task SeedDirectionsAsync(CatalogDbContext db, CancellationToken ct)
    {
        if (await db.Directions.AnyAsync(ct))
        {
            logger.LogDebug("Directions already exist, skipping");
            return;
        }

        var directions = new[]
        {
            Direction.Create("Мальдивы", "Мальдивы", "Райские острова с лазурной водой и белоснежными пляжами"),
            Direction.Create("Пхукет", "Таиланд", "Тропический рай с храмами, пляжами и экзотической кухней"),
            Direction.Create("Санторини", "Греция", "Белые дома на скалах с видом на Эгейское море"),
            Direction.Create("Бали", "Индонезия", "Остров богов с рисовыми террасами и храмами"),
            Direction.Create("Дубай", "ОАЭ", "Современный мегаполис с небоскрёбами и пустыней"),
        };

        db.Directions.AddRange(directions);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} directions", directions.Length);
    }

    private async Task SeedToursAsync(CatalogDbContext db, CancellationToken ct)
    {
        if (await db.Tours.AnyAsync(ct))
        {
            logger.LogDebug("Tours already exist, skipping");
            return;
        }

        var directions = await db.Directions.ToListAsync(ct);
        var maldivesDir = directions.FirstOrDefault(d => d.Country == "Мальдивы");
        var phuketDir = directions.FirstOrDefault(d => d.Name == "Пхукет");
        var greeceDir = directions.FirstOrDefault(d => d.Country == "Греция");

        var now = DateTime.UtcNow;
        // Prices must have ValidFrom <= now && ValidTo >= now for TourListQuery to return minPrice
        var priceStart = now.AddDays(-30);
        var priceEnd = now.AddDays(60);
        var tours = new List<Tour>();

        // Мальдивы — горящий тур
        var maldives = Tour.Create(
            "Мальдивы — рай на земле",
            "Уникальные водные виллы над лазурным океаном. Белоснежный песок, кристально чистая вода и богатый подводный мир.",
            TourType.Beach,
            "Мальдивы",
            7,
            "https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=800",
            maldivesDir?.Id);
        var mPrice = TourOffer.Create(maldives.Id, priceStart, priceEnd, 340_000, "RUB", 12);
        var mPriceHot = TourOffer.Create(maldives.Id, priceStart, priceEnd.AddDays(7), 289_000, "RUB", 8);
        maldives.ReplaceOffers([mPrice, mPriceHot]);
        tours.Add(maldives);

        // Таиланд
        var phuket = Tour.Create(
            "Экзотический Таиланд — Пхукет",
            "Тропический рай с белоснежными пляжами и бирюзовым морем. Экскурсии на острова Пхи-Пхи, храмы и тайская кухня.",
            TourType.Beach,
            "Таиланд",
            11,
            "https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?w=800",
            phuketDir?.Id);
        var pPrice = TourOffer.Create(phuket.Id, priceStart, priceEnd, 124_000, "RUB", 15);
        phuket.ReplaceOffers([pPrice]);
        tours.Add(phuket);

        // Греция
        var santorini = Tour.Create(
            "Санторини — романтика Эгейского моря",
            "Белые дома с синими куполами на вулканических скалах. Закаты, вино и греческая кухня.",
            TourType.Cultural,
            "Греция",
            5,
            "https://images.unsplash.com/photo-1613395877344-13d4a8e0d49e?w=800",
            greeceDir?.Id);
        var sPrice = TourOffer.Create(santorini.Id, priceStart, priceEnd, 89_000, "RUB", 10);
        santorini.ReplaceOffers([sPrice]);
        tours.Add(santorini);

        // Бали
        var bali = Tour.Create(
            "Бали — остров богов",
            "Рисовые террасы Тегаллаланг, храм Танах Лот, пляжи Семиньяк. Йога, спа и индонезийская кухня.",
            TourType.Adventure,
            "Индонезия",
            10,
            "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=800",
            null);
        var bPrice = TourOffer.Create(bali.Id, priceStart, priceEnd, 156_000, "RUB", 8);
        bali.ReplaceOffers([bPrice]);
        tours.Add(bali);

        // Дубай
        var dubai = Tour.Create(
            "Дубай — роскошь и приключения",
            "Бурдж-Халифа, пальмовые острова, пустыня и аквапарки. Шопинг и восточная экзотика.",
            TourType.City,
            "ОАЭ",
            7,
            "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=800",
            null);
        var dPrice = TourOffer.Create(dubai.Id, priceStart, priceEnd, 198_000, "RUB", 20);
        dubai.ReplaceOffers([dPrice]);
        tours.Add(dubai);

        db.Tours.AddRange(tours);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded {Count} tours", tours.Count);
    }
}
