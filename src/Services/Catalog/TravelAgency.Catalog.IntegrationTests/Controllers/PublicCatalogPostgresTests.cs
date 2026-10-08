using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using TravelAgency.Catalog.API.Middleware;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Catalog.IntegrationTests.Helpers;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Catalog.IntegrationTests.Controllers;

[CollectionDefinition(nameof(PublicCatalogPostgresCollection))]
public sealed class PublicCatalogPostgresCollection : ICollectionFixture<PublicCatalogPostgresFixture>;

/// <summary>
/// Фильтры, сортировка и пагинация публичного каталога на Postgres.
/// </summary>
public sealed class PublicCatalogPostgresFixture : IAsyncLifetime
{
    public DateTime Now { get; } = DateTime.UtcNow;
    public Guid DirectionId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public Guid ManagerId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public Guid LowId { get; private set; }
    public Guid MidId { get; private set; }
    public Guid HighId { get; private set; }
    public Guid HighCoverId { get; private set; }
    public Guid ItalyId { get; private set; }
    public Guid DraftId { get; private set; }
    public Guid UnpublishedId { get; private set; }
    public Guid UnpublishedCoverId { get; private set; }
    public Guid UnpublishTargetId { get; private set; }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("catalog_public")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplication? _app;
    private TestServer? _server;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var settings = new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
            ["ConnectionStrings:CatalogDb"] = _postgres.GetConnectionString(),
            ["GrpcSettings:InternalServiceToken"] = "test-internal-token",
            ["GrpcClients:MediaServiceUrl"] = "http://media-service:8081",
            ["Serilog:MinimumLevel:Default"] = "Warning",
            ["ASPNETCORE_RUN_MIGRATIONS"] = "false",
            ["ASPNETCORE_SEED_DATA"] = "false"
        };

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Configuration.AddInMemoryCollection(settings);
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddSingleton<IExceptionMapper, CatalogExceptionMapper>();
        Program.ConfigureServices(builder.Services, builder.Configuration);
        builder.Services.RemoveAll<IMediaFilesClient>();
        builder.Services.AddSingleton<IMediaFilesClient>(new FakeMediaFilesClient());
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "TestIssuer",
                ValidAudience = "TestAudience",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("TestSigningKeyWithAtLeast32CharactersForHMAC")),
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = ClaimTypes.Role
            };
        });

        _app = builder.Build();
        Program.ConfigurePipeline(_app);
        await _app.StartAsync();
        _server = (TestServer)_app.Services.GetRequiredService<IServer>();

        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync();
        Seed(db);
        await db.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _server?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public HttpClient CreateClient() =>
        _server?.CreateClient() ?? throw new InvalidOperationException("Test server is not started.");

    public string Token(string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("TestSigningKeyWithAtLeast32CharactersForHMAC"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, ManagerId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, ManagerId.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<TourStatus> StatusOfAsync(Guid id)
    {
        await using var scope = _app!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        return await db.Tours.AsNoTracking()
            .Where(tour => tour.Id == id)
            .Select(tour => tour.Status)
            .SingleAsync();
    }

    private void Seed(CatalogDbContext db)
    {
        var low = Published("FutureLow", "PublicCatalogSpain", TourType.Beach, null, 100m, 6, 900m, 3);
        var mid = Published("FutureMid", "PublicCatalogSpain", TourType.Beach, null, 200m, 8, 50m, 3);
        var high = Published("FutureHigh", "PublicCatalogSpain", TourType.City, DirectionId, 500m, 20, 10m, 6);
        var italy = Published("ItalyBeach", "PublicCatalogItaly", TourType.Beach, null, 150m, 5, null, 3);
        var hidden = Published("Снятый тур", "PublicCatalogSpain", TourType.Beach, null, 80m, 9, null, 3);
        hidden.Tour.Unpublish();
        var draft = Tour.CreateDraft(ManagerId);
        draft.SetBasics("Черновик каталога", "Кратко", "Москва", "PublicCatalogSpain", TourType.Beach, 1, null);
        var target = Published("Цель снятия", "UnpublishLand", TourType.City, null, 300m, 12, null, 3);

        LowId = low.Tour.Id;
        MidId = mid.Tour.Id;
        HighId = high.Tour.Id;
        HighCoverId = high.CoverId;
        ItalyId = italy.Tour.Id;
        UnpublishedId = hidden.Tour.Id;
        UnpublishedCoverId = hidden.CoverId;
        DraftId = draft.Id;
        UnpublishTargetId = target.Tour.Id;

        db.Tours.AddRange(low.Tour, mid.Tour, high.Tour, italy.Tour, hidden.Tour, draft, target.Tour);
    }

    private (Tour Tour, Guid CoverId) Published(
        string title,
        string country,
        TourType type,
        Guid? directionId,
        decimal futurePrice,
        int daysAhead,
        decimal? currentPrice,
        int imageCount)
    {
        var tour = Tour.CreateDraft(ManagerId);
        tour.SetBasics(title, "Кратко о туре", "Москва", country, type, 1, directionId);
        tour.SetDescription($"Описание {title}");
        tour.ReplaceDays([TourDay.Create(tour.Id, 1, "День", "Программа дня")]);
        tour.ReplaceConditions(
            [TourInclusion.Create(tour.Id, "Перелёт", TourInclusionKind.Included)],
            MealPlan.BB,
            "Отель");

        var departure = Now.AddDays(daysAhead);
        var offers = new List<TourOffer>
        {
            TourOffer.Create(tour.Id, departure, departure.AddDays(3), futurePrice, "EUR", 4)
        };
        if (currentPrice is decimal current)
            offers.Insert(0, TourOffer.Create(tour.Id, Now.AddDays(-1), Now.AddDays(2), current, "EUR", 4));
        tour.ReplaceOffers(offers);

        var coverId = Guid.NewGuid();
        var images = new List<TourImage>
        {
            TourImage.Create(tour.Id, coverId, 0, true, "Обложка", 1600)
        };
        for (var index = 1; index < imageCount; index++)
            images.Add(TourImage.Create(tour.Id, Guid.NewGuid(), index, false, $"Фото {index}", 800));
        tour.ReplaceImages(images, 1600);
        tour.Publish(Now, 1600);
        return (tour, coverId);
    }
}

[Collection(nameof(PublicCatalogPostgresCollection))]
public sealed class PublicCatalogPostgresTests(PublicCatalogPostgresFixture fixture)
{
    private const string Spain = "country=publiccatalogspain";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task List_ShowsOnlyPublished_ToAnonymousAndClient()
    {
        var anonymous = await GetAsync($"/catalog/tours?{Spain}&pageSize=20");
        var client = await GetAsync($"/catalog/tours?{Spain}&pageSize=20&isActive=false", "Client");

        foreach (var body in new[] { anonymous, client })
        {
            body.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await body.Content.ReadFromJsonAsync<PagedResult<TourSummaryDto>>();
            page!.TotalCount.Should().Be(3);
            var ids = page.Items.Select(item => item.Id).ToList();
            ids.Should().BeEquivalentTo([fixture.LowId, fixture.MidId, fixture.HighId]);
            ids.Should().NotContain(fixture.DraftId);
            ids.Should().NotContain(fixture.UnpublishedId);
        }
    }

    [Fact]
    public async Task List_FiltersByCountryPriceTypeDirectionAndFutureDate()
    {
        var byPrice = await ReadListAsync($"/catalog/tours?{Spain}&maxPrice=150");
        byPrice.Items.Select(item => item.Id).Should().Equal(fixture.LowId);

        var expensive = await ReadListAsync($"/catalog/tours?{Spain}&minPrice=400");
        expensive.Items.Select(item => item.Id).Should().Equal(fixture.HighId);
        expensive.Items.Single().PriceFrom.Should().Be(500m);
        expensive.Items.Single().MinPrice.Should().Be(500m);

        var city = await ReadListAsync($"/catalog/tours?{Spain}&tourType=City");
        city.Items.Select(item => item.Id).Should().Equal(fixture.HighId);

        var direction = await ReadListAsync($"/catalog/tours?{Spain}&directionId={fixture.DirectionId}");
        direction.Items.Select(item => item.Id).Should().Equal(fixture.HighId);

        var from = Uri.EscapeDataString(fixture.Now.AddDays(10).ToString("O", CultureInfo.InvariantCulture));
        var later = await ReadListAsync($"/catalog/tours?{Spain}&dateFrom={from}");
        later.Items.Select(item => item.Id).Should().Equal(fixture.HighId);

        var countries = await ReadListAsync("/catalog/tours?country=publiccatalogitaly");
        countries.Items.Select(item => item.Id).Should().Equal(fixture.ItalyId);
    }

    [Fact]
    public async Task List_SortsByFuturePrice_AndPaginates()
    {
        var first = await ReadListAsync($"/catalog/tours?{Spain}&sortBy=Price&sortDirection=Asc&page=1&pageSize=1");
        first.TotalCount.Should().Be(3);
        first.Items.Should().ContainSingle();
        first.Items.Single().Id.Should().Be(fixture.LowId);
        first.Items.Single().PriceFrom.Should().Be(100m);

        var second = await ReadListAsync($"/catalog/tours?{Spain}&sortBy=Price&sortDirection=Asc&page=2&pageSize=1");
        second.Items.Single().Id.Should().Be(fixture.MidId);

        var third = await ReadListAsync($"/catalog/tours?{Spain}&sortBy=Price&sortDirection=Asc&page=3&pageSize=1");
        third.Items.Single().Id.Should().Be(fixture.HighId);

        var byTitle = await ReadListAsync($"/catalog/tours?{Spain}&sortBy=Title&sortDirection=Asc&pageSize=10");
        byTitle.Items.Select(item => item.Title).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task List_ReturnsAtMostFivePreviews_WithCoverFirst()
    {
        var page = await ReadListAsync($"/catalog/tours?{Spain}&pageSize=20");
        var high = page.Items.Single(item => item.Id == fixture.HighId);

        high.Previews.Should().NotBeNull();
        high.Previews.Should().HaveCount(5);
        high.Previews![0].IsCover.Should().BeTrue();
        high.Previews[0].MediaFileId.Should().Be(fixture.HighCoverId);
        high.NearestDate.Should().BeCloseTo(fixture.Now.AddDays(20), TimeSpan.FromSeconds(1));
        high.Previews[0].Url.Should().Be($"/api/v1/media/files/{fixture.HighCoverId:D}/w800");
    }

    [Fact]
    public async Task Detail_ReturnsPublishedTour_AndHidesDraftAndUnpublished()
    {
        var response = await GetAsync($"/catalog/tours/{fixture.HighId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("components").GetArrayLength().Should().Be(0);

        var tour = JsonSerializer.Deserialize<PublicTourDto>(json, JsonOptions);
        tour!.Title.Should().Be("FutureHigh");
        tour.DepartureCity.Should().Be("Москва");
        tour.MealPlan.Should().Be(nameof(MealPlan.BB));
        tour.AccommodationText.Should().Be("Отель");
        tour.Days.Should().ContainSingle();
        tour.Inclusions.Should().Contain(item => item.Text == "Перелёт");
        tour.PriceFrom.Should().Be(500m);
        tour.Prices.Select(price => price.PricePerPerson).Should().NotContain(10m);
        tour.NearestDate.Should().BeCloseTo(fixture.Now.AddDays(20), TimeSpan.FromSeconds(1));
        tour.Images.Should().HaveCount(6);

        foreach (var role in new string?[] { null, "Client" })
        {
            (await GetAsync($"/catalog/tours/{fixture.DraftId}", role)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await GetAsync($"/catalog/tours/{fixture.UnpublishedId}", role)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }

    [Fact]
    public async Task Cards_ReturnsPublishedAndMinimalUnpublished_SkipsDrafts_AndRejectsTooManyIds()
    {
        var missing = Guid.NewGuid();
        var url =
            $"/catalog/tours/cards?ids={fixture.UnpublishedId}&ids={fixture.LowId}&ids={fixture.DraftId}&ids={missing}";
        var response = await GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        var items = document.RootElement.EnumerateArray().ToList();
        items.Should().HaveCount(2);
        items[0].GetProperty("id").GetGuid().Should().Be(fixture.UnpublishedId);
        items[0].GetProperty("available").GetBoolean().Should().BeFalse();
        items[0].GetProperty("title").GetString().Should().Be("Снятый тур");
        items[0].GetProperty("cover").GetProperty("mediaFileId").GetGuid().Should().Be(fixture.UnpublishedCoverId);
        items[0].TryGetProperty("priceFrom", out _).Should().BeFalse();
        items[1].GetProperty("id").GetGuid().Should().Be(fixture.LowId);
        items[1].GetProperty("available").GetBoolean().Should().BeTrue();
        items[1].GetProperty("priceFrom").GetDecimal().Should().Be(100m);

        var empty = await GetAsync("/catalog/tours/cards");
        empty.StatusCode.Should().Be(HttpStatusCode.OK);
        (await empty.Content.ReadFromJsonAsync<List<PublicTourCardDto>>()).Should().BeEmpty();

        var tooMany = string.Join("&", Enumerable.Range(0, 51).Select(_ => "ids=" + Guid.NewGuid()));
        (await GetAsync("/catalog/tours/cards?" + tooMany)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetAsync("/catalog/tours/cards?ids=not-a-guid")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Unpublish_WithoutIfMatch_OnPublishedTour_Returns428_AndStaysPublished()
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fixture.Token("Manager"));
        var response = await client.PostAsync($"/catalog/manage/tours/{fixture.UnpublishTargetId}/unpublish", null);

        response.StatusCode.Should().Be(HttpStatusCode.PreconditionRequired);
        (await fixture.StatusOfAsync(fixture.UnpublishTargetId)).Should().Be(TourStatus.Published);
    }

    private async Task<PagedResult<TourSummaryDto>> ReadListAsync(string url)
    {
        var response = await GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<TourSummaryDto>>();
        page.Should().NotBeNull();
        return page!;
    }

    private async Task<HttpResponseMessage> GetAsync(string url, string? role = null)
    {
        var client = fixture.CreateClient();
        if (role is not null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", fixture.Token(role));
        }

        return await client.GetAsync(url);
    }
}
