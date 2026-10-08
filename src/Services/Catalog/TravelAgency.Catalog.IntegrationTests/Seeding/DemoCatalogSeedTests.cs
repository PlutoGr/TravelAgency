using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Catalog.Infrastructure.Seeding;
using TravelAgency.Shared.Contracts.Seeding;
using TravelAgency.Shared.Infrastructure.Seeding;

namespace TravelAgency.Catalog.IntegrationTests.Seeding;

public class DemoCatalogSeedTests
{
    [Fact]
    public async Task Seed_PublishesFiveTours_AndSecondRunDoesNotDuplicate()
    {
        await using var db = await CreateDbAsync();

        await CatalogDataSeeder.SeedAsync(db, NullLogger.Instance, CancellationToken.None);
        await CatalogDataSeeder.SeedAsync(db, NullLogger.Instance, CancellationToken.None);

        var tours = await db.Tours.AsNoTracking()
            .Include(t => t.Images)
            .Include(t => t.Days)
            .Include(t => t.Offers)
            .Include(t => t.Inclusions)
            .ToListAsync();

        tours.Should().HaveCount(DemoSeedIds.PublishedTourCount + 1);
        var draft = tours.Single(t => t.Id == DemoSeedIds.Manager2DraftTourId);
        draft.Status.Should().Be(TourStatus.Draft);
        draft.OwnerId.Should().Be(DemoSeedIds.Manager2Id);
        draft.Title.Should().Be(CatalogDataSeeder.Manager2DraftTitle);

        foreach (var id in DemoSeedIds.PublishedTourIds)
        {
            var tour = tours.Single(t => t.Id == id);
            tour.Status.Should().Be(TourStatus.Published);
            tour.OwnerId.Should().Be(DemoSeedIds.ManagerId);
            tour.Source.Should().Be(TourSource.Manager);
            tour.Images.Should().HaveCount(DemoSeedIds.PhotosPerTour);
            tour.Images.Should().ContainSingle(i => i.IsCover);
            tour.Images.Single(i => i.IsCover).WidthPx.Should().BeGreaterThanOrEqualTo(TourContentLimits.CoverMinWidthPx);
            tour.GetMissingPublishRequirements(DateTime.UtcNow).Should().BeEmpty();
            tour.ImageUrl.Should().NotBeNull();
        }

        var maldivesId = DemoSeedIds.MaldivesTourId;
        var images = await db.TourImages.Where(i => i.TourId == maldivesId).ToListAsync();
        db.TourImages.RemoveRange(images);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await CatalogDataSeeder.SeedAsync(db, NullLogger.Instance, CancellationToken.None);

        var restored = await db.Tours.AsNoTracking().Include(t => t.Images).SingleAsync(t => t.Id == maldivesId);
        restored.Images.Should().HaveCount(DemoSeedIds.PhotosPerTour);
        restored.Status.Should().Be(TourStatus.Published);
        (await db.Tours.CountAsync()).Should().Be(DemoSeedIds.PublishedTourCount + 1);
    }

    [Fact]
    public async Task Production_DoesNotSeed_EvenWhenTheFlagIsSet()
    {
        await using var db = await CreateDbAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DemoSeedGate.SeedDataKey] = "true",
            [DemoSeedGate.DemoCatalogKey] = "true"
        }).Build();
        var seeder = new CatalogDataSeeder(
            Scope(db),
            new TestEnvironment(Environments.Production),
            config,
            NullLogger<CatalogDataSeeder>.Instance);

        await seeder.StartAsync(CancellationToken.None);

        (await db.Tours.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ManagersGet403OnEachOthersTours_AdminReadsAndEditsBoth()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        factory.EnsureDbCreated();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await CatalogDataSeeder.SeedAsync(db, NullLogger.Instance, CancellationToken.None);

        var client = factory.CreateClient();
        var published = DemoSeedIds.MaldivesTourId;
        var draft = DemoSeedIds.Manager2DraftTourId;

        Authorize(factory, client, DemoSeedIds.ManagerId, "Manager");
        (await client.GetAsync($"/catalog/manage/tours/{draft}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Put, $"/catalog/manage/tours/{draft}/basics", BasicsBody(), "\"1\""))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        Authorize(factory, client, DemoSeedIds.Manager2Id, "Manager");
        (await client.GetAsync($"/catalog/manage/tours/{published}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Put, $"/catalog/manage/tours/{published}/basics", BasicsBody(), "\"1\""))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync($"/catalog/manage/tours/{draft}")).StatusCode.Should().Be(HttpStatusCode.OK);

        Authorize(factory, client, DemoSeedIds.AdminId, "Admin");
        var managerTour = await client.GetFromJsonAsync<TourManageDto>($"/catalog/manage/tours/{published}");
        var manager2Tour = await client.GetFromJsonAsync<TourManageDto>($"/catalog/manage/tours/{draft}");
        managerTour.Should().NotBeNull();
        manager2Tour.Should().NotBeNull();

        var editedManager = await SendAsync(
            client,
            HttpMethod.Put,
            $"/catalog/manage/tours/{published}/basics",
            new UpdateTourBasicsRequest(
                managerTour!.Title,
                managerTour.ShortDescription,
                "Казань",
                managerTour.Country,
                managerTour.TourType,
                managerTour.DurationDays,
                managerTour.DirectionId),
            managerTour.Etag);
        editedManager.StatusCode.Should().Be(HttpStatusCode.OK);

        var editedDraft = await SendAsync(
            client,
            HttpMethod.Put,
            $"/catalog/manage/tours/{draft}/basics",
            new UpdateTourBasicsRequest(
                manager2Tour!.Title,
                manager2Tour.ShortDescription,
                "Казань",
                "Италия",
                nameof(TourType.City),
                1,
                null),
            manager2Tour.Etag);
        editedDraft.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await client.GetFromJsonAsync<PagedResult<TourSummaryDto>>("/catalog/tours");
        list.Should().NotBeNull();
        list!.Items.Select(t => t.Id).Should().NotContain(draft);
        list.Items.Select(t => t.Id).Should().Contain(DemoSeedIds.PublishedTourIds);
    }

    [Fact]
    public async Task Admin_PublishesManagerTourWithOwnerPhotos_AndRejectsOwnerlessTourWithForeignPhotos()
    {
        await using var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();
        factory.EnsureDbCreated();

        Guid ownedId;
        Guid ownerlessId;
        string ownedEtag;
        string ownerlessEtag;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var owned = PublishableDraft(DemoSeedIds.ManagerId, "Тур менеджера");
            var ownerless = PublishableDraft(null, "Тур без владельца");
            db.Tours.AddRange(owned, ownerless);
            await db.SaveChangesAsync();
            ownedId = owned.Id;
            ownerlessId = ownerless.Id;
            ownedEtag = $"\"{owned.Version}\"";
            ownerlessEtag = $"\"{ownerless.Version}\"";
            factory.Media.Files = owned.Images.Concat(ownerless.Images)
                .Select(image => image.MediaFileId)
                .Distinct()
                .ToDictionary(
                    id => id,
                    id => new RemoteMediaFile(id, DemoSeedIds.ManagerId.ToString(), DemoSeedIds.PhotoWidthPx, 900));
        }

        var client = factory.CreateClient();
        Authorize(factory, client, DemoSeedIds.AdminId, "Admin");

        var published = await SendAsync(
            client,
            HttpMethod.Post,
            $"/catalog/manage/tours/{ownedId}/publish",
            new { },
            ownedEtag);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await published.Content.ReadFromJsonAsync<TourManageDto>();
        body.Should().NotBeNull();
        body!.Status.Should().Be(nameof(TourStatus.Published));
        body.OwnerId.Should().Be(DemoSeedIds.ManagerId);

        var rejected = await SendAsync(
            client,
            HttpMethod.Post,
            $"/catalog/manage/tours/{ownerlessId}/publish",
            new { },
            ownerlessEtag);
        rejected.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await rejected.Content.ReadAsStringAsync();
        problem.Should().Contain("tour-image");
        problem.Should().Contain("A media file belongs to another user");

        factory.UseDbContext(db =>
        {
            var stored = db.Tours.Single(t => t.Id == ownerlessId);
            stored.OwnerId.Should().BeNull();
            stored.Status.Should().Be(TourStatus.Draft);
        });
    }

    private static Tour PublishableDraft(Guid? ownerId, string title)
    {
        var tour = Tour.Create(
            title,
            "Полное описание тура для проверки публикации.",
            TourType.Beach,
            "Индонезия",
            1,
            null,
            ownerId: ownerId);
        tour.SetBasics(title, "Кратко о туре", "Москва", "Индонезия", TourType.Beach, 1, null);
        tour.SetDescription("Полное описание тура для проверки публикации.");
        tour.ReplaceDays([TourDay.Create(tour.Id, 1, "День 1", "Программа")]);
        tour.ReplaceConditions(
            [TourInclusion.Create(tour.Id, "Проживание", TourInclusionKind.Included, 0)],
            MealPlan.BB,
            "Вилла");
        var from = DateTime.UtcNow.AddDays(30);
        tour.ReplaceOffers([TourOffer.Create(tour.Id, from, from.AddDays(7), 1000m, "RUB", 4)]);
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        tour.ReplaceImages(
        [
            TourImage.Create(tour.Id, ids[0], 0, true, "Обложка", DemoSeedIds.PhotoWidthPx),
            TourImage.Create(tour.Id, ids[1], 1, false, "Второе", 800),
            TourImage.Create(tour.Id, ids[2], 2, false, "Третье", 800)
        ], DemoSeedIds.PhotoWidthPx);
        return tour;
    }

    private static UpdateTourBasicsRequest BasicsBody() =>
        new("Чужой", null, null, null, null, null, null);

    private static void Authorize(CustomWebApplicationFactory factory, HttpClient client, Guid userId, string role)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", factory.GenerateToken(userId.ToString(), role));
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, object body, string etag)
    {
        var request = new HttpRequestMessage(method, url) { Content = JsonContent.Create(body) };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }

    private static async Task<CatalogDbContext> CreateDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<CatalogDbContext>().UseSqlite(connection).Options;
        var db = new CatalogDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static IServiceScopeFactory Scope(CatalogDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
