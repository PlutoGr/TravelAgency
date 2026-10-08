using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Catalog.Infrastructure.Seeding;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Seeding;
using TravelAgency.Identity.Infrastructure.Services;
using TravelAgency.Shared.Contracts.Seeding;
using TravelAgency.Shared.Infrastructure.Seeding;

namespace TravelAgency.Catalog.IntegrationTests.Seeding;

/// <summary>
/// На dev уже лежат демо-туры и аккаунты старого сидера: случайные id, без фото, не опубликованы,
/// у туров есть предложения, на которые ссылаются брони. Сидер дописывает недостающее и не плодит дубли.
/// </summary>
public sealed class LegacyDemoSeedPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("catalog_legacy_seed")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task LegacyRows_KeepTourAndOfferIds_AndSecondRunDoesNotDuplicate()
    {
        var catalogConnection = _postgres.GetConnectionString();
        var identityConnection = await CreateIdentityDatabaseAsync(catalogConnection);
        var catalogOptions = new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(catalogConnection).Options;
        var identityOptions = new DbContextOptionsBuilder<IdentityDbContext>().UseNpgsql(identityConnection).Options;

        await using (var catalog = new CatalogDbContext(catalogOptions))
            await catalog.Database.MigrateAsync();
        await using (var identity = new IdentityDbContext(identityOptions))
            await identity.Database.MigrateAsync();

        var legacyUserIds = await SeedLegacyUsersAsync(identityOptions);
        var legacy = await SeedLegacyToursAsync(catalogOptions);

        var password = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DemoSeedGate.TestUserPasswordKey] = "fixture-secret"
        }).Build();
        var hasher = new PasswordHasherService();

        await using (var identity = new IdentityDbContext(identityOptions))
            await IdentityDataSeeder.SeedAsync(identity, hasher, password, NullLogger.Instance, CancellationToken.None);
        await using (var identity = new IdentityDbContext(identityOptions))
            await IdentityDataSeeder.SeedAsync(identity, hasher, password, NullLogger.Instance, CancellationToken.None);

        await using (var catalog = new CatalogDbContext(catalogOptions))
            await CatalogDataSeeder.SeedAsync(catalog, NullLogger.Instance, CancellationToken.None);
        await using (var catalog = new CatalogDbContext(catalogOptions))
            await CatalogDataSeeder.SeedAsync(catalog, NullLogger.Instance, CancellationToken.None);

        await using var usersDb = new IdentityDbContext(identityOptions);
        var users = await usersDb.Users.AsNoTracking().ToListAsync();
        users.Select(u => u.Email).Should().OnlyHaveUniqueItems();
        users.Should().HaveCount(4);
        users.Single(u => u.Email == "client@test.com").Id.Should().Be(legacyUserIds.ClientId);
        users.Single(u => u.Email == "manager@test.com").Id.Should().Be(legacyUserIds.ManagerId);
        users.Single(u => u.Email == "admin@test.com").Id.Should().Be(legacyUserIds.AdminId);
        users.Single(u => u.Email == "manager2@test.com").Id.Should().Be(DemoSeedIds.Manager2Id);

        await using var toursDb = new CatalogDbContext(catalogOptions);
        var tours = await toursDb.Tours.AsNoTracking()
            .Include(t => t.Offers)
            .Include(t => t.Images)
            .Include(t => t.Days)
            .Include(t => t.Inclusions)
            .ToListAsync();

        tours.Should().HaveCount(DemoSeedIds.PublishedTourCount + 1);
        tours.Select(t => t.Title).Should().OnlyHaveUniqueItems();
        tours.Count(t => t.Status == TourStatus.Published).Should().Be(DemoSeedIds.PublishedTourCount);
        var draft = tours.Single(t => t.Status == TourStatus.Draft);
        draft.Title.Should().Be(CatalogDataSeeder.Manager2DraftTitle);
        draft.OwnerId.Should().Be(DemoSeedIds.Manager2Id);
        draft.Id.Should().Be(DemoSeedIds.Manager2DraftTourId);

        var offers = await toursDb.TourOffers.AsNoTracking().ToListAsync();
        offers.Should().HaveCount(legacy.Offers.Count + DemoSeedIds.PublishedTourCount);

        foreach (var snapshot in legacy.Offers)
        {
            var offer = offers.Single(o => o.Id == snapshot.Id);
            offer.TourId.Should().Be(snapshot.TourId);
            offer.PricePerPerson.Should().Be(snapshot.PricePerPerson);
            offer.Currency.Should().Be(snapshot.Currency);
            offer.AvailableSeats.Should().Be(snapshot.AvailableSeats);
            Utc(offer.ValidFrom).Should().Be(snapshot.ValidFrom);
            Utc(offer.ValidTo).Should().Be(snapshot.ValidTo);
        }

        foreach (var snapshot in legacy.Tours)
        {
            var tour = tours.Single(t => t.Title == snapshot.Title);
            tour.Id.Should().Be(snapshot.Id);
            tour.Status.Should().Be(TourStatus.Published);
            tour.OwnerId.Should().Be(DemoSeedIds.ManagerId);
            tour.Images.Should().HaveCount(DemoSeedIds.PhotosPerTour);
            tour.GetMissingPublishRequirements(DateTime.UtcNow).Should().BeEmpty();
            tour.Offers.Select(o => o.Id).Should().Contain(legacy.Offers.Where(o => o.TourId == snapshot.Id).Select(o => o.Id));
        }
    }

    [Fact]
    public async Task ReadyNullOwner_BecomesManager_ForeignOwnerStays_SecondRunKeepsIdsAndVersion()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
        await using (var catalog = new CatalogDbContext(options))
            await catalog.Database.MigrateAsync();

        var foreignOwner = Guid.Parse("55555555-5555-4555-8555-555555555555");
        FrozenTour bali;
        FrozenTour dubai;
        FrozenTour santorini;
        FrozenTour phuket;

        await using (var db = new CatalogDbContext(options))
        {
            var baliTour = CompletePublishedTour("Бали — остров богов", 4, ownerId: null);
            var dubaiTour = CompletePublishedTour("Дубай — роскошь и приключения", 5, foreignOwner);
            var santoriniTour = CompletePublishedTour("Санторини — романтика Эгейского моря", 3, DemoSeedIds.ManagerId);
            var phuketTour = Tour.Create(
                "Экзотический Таиланд — Пхукет",
                "Тропический рай с белоснежными пляжами.",
                TourType.Beach,
                "Таиланд",
                11,
                "https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?w=800",
                ownerId: foreignOwner);
            var keptOffer = TourOffer.Create(
                phuketTour.Id,
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                124_000,
                "RUB",
                15);
            phuketTour.AddOffer(keptOffer);
            db.Tours.AddRange(baliTour, dubaiTour, santoriniTour, phuketTour);
            await db.SaveChangesAsync();
            bali = Freeze(baliTour);
            dubai = Freeze(dubaiTour);
            santorini = Freeze(santoriniTour);
            phuket = Freeze(phuketTour);
        }

        await using (var catalog = new CatalogDbContext(options))
            await CatalogDataSeeder.SeedAsync(catalog, NullLogger.Instance, CancellationToken.None);

        await using (var catalog = new CatalogDbContext(options))
        {
            var tours = await LoadToursAsync(catalog);
            var seededBali = tours.Single(t => t.Id == bali.Id);
            seededBali.OwnerId.Should().Be(DemoSeedIds.ManagerId);
            seededBali.Status.Should().Be(TourStatus.Published);
            seededBali.Version.Should().Be(bali.Version + 1);
            SameRows(seededBali, bali);

            var seededDubai = tours.Single(t => t.Id == dubai.Id);
            seededDubai.OwnerId.Should().Be(foreignOwner);
            seededDubai.Version.Should().Be(dubai.Version);
            SameRows(seededDubai, dubai);

            var seededSantorini = tours.Single(t => t.Id == santorini.Id);
            seededSantorini.OwnerId.Should().Be(DemoSeedIds.ManagerId);
            seededSantorini.Version.Should().Be(santorini.Version);
            SameRows(seededSantorini, santorini);

            var seededPhuket = tours.Single(t => t.Id == phuket.Id);
            seededPhuket.OwnerId.Should().Be(foreignOwner);
            seededPhuket.Status.Should().Be(TourStatus.Published);
            seededPhuket.Offers.Select(o => o.Id).Should().Contain(phuket.OfferIds);

            tours.Should().ContainSingle(t => t.Status == TourStatus.Draft);
            tours.Single(t => t.Id == DemoSeedIds.Manager2DraftTourId).OwnerId.Should().Be(DemoSeedIds.Manager2Id);
            tours.Select(t => t.Title).Should().OnlyHaveUniqueItems();
        }

        await using (var catalog = new CatalogDbContext(options))
            await CatalogDataSeeder.SeedAsync(catalog, NullLogger.Instance, CancellationToken.None);

        await using (var catalog = new CatalogDbContext(options))
        {
            var tours = await LoadToursAsync(catalog);
            var againBali = tours.Single(t => t.Id == bali.Id);
            againBali.OwnerId.Should().Be(DemoSeedIds.ManagerId);
            againBali.Version.Should().Be(bali.Version + 1);
            SameRows(againBali, bali);

            var againDubai = tours.Single(t => t.Id == dubai.Id);
            againDubai.OwnerId.Should().Be(foreignOwner);
            againDubai.Version.Should().Be(dubai.Version);
            SameRows(againDubai, dubai);

            var againSantorini = tours.Single(t => t.Id == santorini.Id);
            againSantorini.OwnerId.Should().Be(DemoSeedIds.ManagerId);
            againSantorini.Version.Should().Be(santorini.Version);

            var againPhuket = tours.Single(t => t.Id == phuket.Id);
            againPhuket.OwnerId.Should().Be(foreignOwner);
            againPhuket.Offers.Select(o => o.Id).Should().Contain(phuket.OfferIds);
            tours.Single(t => t.Status == TourStatus.Draft).Id.Should().Be(DemoSeedIds.Manager2DraftTourId);
        }
    }

    private static Tour CompletePublishedTour(string title, int tourNumber, Guid? ownerId)
    {
        var tour = Tour.Create(
            title,
            "Описание демо-тура для проверки владельца.",
            TourType.Beach,
            "Индонезия",
            1,
            "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=800",
            ownerId: ownerId);
        tour.SetBasics(title, "Краткое описание", "Москва", "Индонезия", TourType.Beach, 1, null);
        tour.SetDescription("Описание демо-тура для проверки владельца.");
        tour.ReplaceDays([TourDay.Create(tour.Id, 1, "День 1", "Программа")]);
        tour.ReplaceConditions(
            [TourInclusion.Create(tour.Id, "Проживание", TourInclusionKind.Included, 0)],
            MealPlan.BB,
            "Вилла");
        var future = DateTime.UtcNow.AddDays(30);
        tour.AddOffer(TourOffer.Create(tour.Id, future, future.AddDays(7), 1000m, "RUB", 4));
        for (var photo = 1; photo <= DemoSeedIds.PhotosPerTour; photo++)
        {
            tour.AddImage(TourImage.Create(
                tour.Id,
                DemoSeedIds.PhotoId(tourNumber, photo),
                photo - 1,
                photo == 1,
                $"Фото {photo}",
                DemoSeedIds.PhotoWidthPx));
        }

        tour.Publish(DateTime.UtcNow, DemoSeedIds.PhotoWidthPx);
        return tour;
    }

    private static async Task<List<Tour>> LoadToursAsync(CatalogDbContext db) =>
        await db.Tours.AsNoTracking()
            .Include(t => t.Offers)
            .Include(t => t.Images)
            .ToListAsync();

    private static FrozenTour Freeze(Tour tour) =>
        new(
            tour.Id,
            tour.Version,
            tour.Offers.Select(o => o.Id).ToArray(),
            tour.Images.Select(i => i.Id).ToArray());

    private static void SameRows(Tour tour, FrozenTour snapshot)
    {
        tour.Offers.Select(o => o.Id).Should().BeEquivalentTo(snapshot.OfferIds);
        tour.Images.Select(i => i.Id).Should().BeEquivalentTo(snapshot.ImageIds);
    }

    private sealed record FrozenTour(Guid Id, long Version, Guid[] OfferIds, Guid[] ImageIds);

    private static async Task<string> CreateIdentityDatabaseAsync(string catalogConnection)
    {
        var identity = new NpgsqlConnectionStringBuilder(catalogConnection) { Database = "travel_identity" };
        await using var connection = new NpgsqlConnection(catalogConnection);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("CREATE DATABASE travel_identity", connection);
        await command.ExecuteNonQueryAsync();
        return identity.ConnectionString;
    }

    private static async Task<(Guid ClientId, Guid ManagerId, Guid AdminId)> SeedLegacyUsersAsync(
        DbContextOptions<IdentityDbContext> options)
    {
        await using var db = new IdentityDbContext(options);
        var client = User.Create("client@test.com", "legacy-hash", "Клиент", "Тестов", null, UserRole.Client);
        var manager = User.Create("manager@test.com", "legacy-hash", "Менеджер", "Тестов", null, UserRole.Manager);
        var admin = User.Create("admin@test.com", "legacy-hash", "Админ", "Тестов", null, UserRole.Admin);
        db.Users.AddRange(client, manager, admin);
        await db.SaveChangesAsync();
        return (client.Id, manager.Id, admin.Id);
    }

    private static async Task<LegacyCatalog> SeedLegacyToursAsync(DbContextOptions<CatalogDbContext> options)
    {
        await using var db = new CatalogDbContext(options);
        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc);
        var specs = new (string Title, string Description, TourType Type, string Country, int Days, string ImageUrl, decimal Price, int Seats, bool SecondOffer)[]
        {
            ("Мальдивы — рай на земле", "Уникальные водные виллы над лазурным океаном.", TourType.Beach, "Мальдивы", 7, "https://images.unsplash.com/photo-1514282401047-d79a71a590e8?w=800", 289_000, 8, true),
            ("Экзотический Таиланд — Пхукет", "Тропический рай с белоснежными пляжами.", TourType.Beach, "Таиланд", 11, "https://images.unsplash.com/photo-1552465011-b4e21bf6e79a?w=800", 124_000, 15, false),
            ("Санторини — романтика Эгейского моря", "Белые дома с синими куполами.", TourType.Cultural, "Греция", 5, "https://images.unsplash.com/photo-1613395877344-13d4a8e0d49e?w=800", 89_000, 10, false),
            ("Бали — остров богов", "Рисовые террасы и храмы.", TourType.Adventure, "Индонезия", 10, "https://images.unsplash.com/photo-1537996194471-e657df975ab4?w=800", 156_000, 8, false),
            ("Дубай — роскошь и приключения", "Бурдж-Халифа и пустыня.", TourType.City, "ОАЭ", 7, "https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=800", 198_000, 20, false)
        };

        var tours = new List<TourSnapshot>();
        var offers = new List<OfferSnapshot>();
        foreach (var spec in specs)
        {
            var tour = Tour.Create(spec.Title, spec.Description, spec.Type, spec.Country, spec.Days, spec.ImageUrl);
            tour.AddOffer(TourOffer.Create(tour.Id, from, to, spec.Price, "RUB", spec.Seats));
            if (spec.SecondOffer)
                tour.AddOffer(TourOffer.Create(
                    tour.Id,
                    from,
                    new DateTime(2026, 12, 8, 0, 0, 0, DateTimeKind.Utc),
                    340_000,
                    "RUB",
                    12));

            db.Tours.Add(tour);
            tours.Add(new TourSnapshot(tour.Id, tour.Title));
            offers.AddRange(tour.Offers.Select(offer => new OfferSnapshot(
                offer.Id, offer.TourId, offer.ValidFrom, offer.ValidTo, offer.PricePerPerson, offer.Currency, offer.AvailableSeats)));
        }

        await db.SaveChangesAsync();
        return new LegacyCatalog(tours, offers);
    }

    private sealed record LegacyCatalog(IReadOnlyList<TourSnapshot> Tours, IReadOnlyList<OfferSnapshot> Offers);

    private sealed record TourSnapshot(Guid Id, string Title);

    private static DateTime Utc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed record OfferSnapshot(
        Guid Id,
        Guid TourId,
        DateTime ValidFrom,
        DateTime ValidTo,
        decimal PricePerPerson,
        string Currency,
        int AvailableSeats);
}
