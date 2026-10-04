using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.IntegrationTests.Controllers;

[Collection(nameof(CatalogIntegrationTestCollection))]
public class TourManageControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _managerId = Guid.NewGuid();

    public TourManageControllerTests(CatalogTestFixture fixture)
    {
        _factory = fixture.Factory;
        _client = fixture.Factory.CreateClient();
        _factory.Media.Reset(_managerId.ToString());
        Authorize(_managerId, "Manager");
    }

    [Fact]
    public async Task Wizard_SavesEachStep_Publishes_AndRejectsMissingPreconditions()
    {
        var created = await CreateDraftAsync();
        created.Status.Should().Be(nameof(TourStatus.Draft));
        created.Source.Should().Be(nameof(TourSource.Manager));
        created.OwnerId.Should().Be(_managerId);

        var basics = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/basics",
            new UpdateTourBasicsRequest("Сочи", "Коротко", "Москва", "Россия", nameof(TourType.Beach), 2, null),
            created.Etag);
        basics.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterBasics = await ReadTourAsync(basics);

        var withoutMatch = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/description",
            new UpdateTourDescriptionRequest("Полное описание моря"),
            null);
        withoutMatch.StatusCode.Should().Be(HttpStatusCode.PreconditionRequired);

        var stale = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/description",
            new UpdateTourDescriptionRequest("Полное описание моря"),
            "\"0\"");
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var described = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/description",
            new UpdateTourDescriptionRequest("Полное описание моря"),
            afterBasics.Etag);
        described.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterDescription = await ReadTourAsync(described);

        var programmed = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/program",
            new UpdateTourProgramRequest(
            [
                new TourDayInput(1, "Прилёт", "Встреча"),
                new TourDayInput(2, "Пляж", "Море")
            ]),
            afterDescription.Etag);
        programmed.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterProgram = await ReadTourAsync(programmed);

        var conditions = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/conditions",
            new UpdateTourConditionsRequest(
                [new TourInclusionInput("Перелёт", nameof(TourInclusionKind.Included), 0)],
                nameof(MealPlan.BB),
                "Отель у моря"),
            afterProgram.Etag);
        conditions.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterConditions = await ReadTourAsync(conditions);

        var from = DateTime.UtcNow.AddDays(20);
        var prices = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/prices",
            new UpdateTourManagePricesRequest(
            [
                new TourOfferInput(from, from.AddDays(2), 45000m, "RUB", 10)
            ]),
            afterConditions.Etag);
        prices.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterPrices = await ReadTourAsync(prices);

        var cover = Guid.NewGuid();
        var second = Guid.NewGuid();
        var images = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/images",
            new UpdateTourImagesRequest(
            [
                new TourImageInput(cover, 0, true, "Обложка"),
                new TourImageInput(second, 1, false, "Второе")
            ]),
            afterPrices.Etag);
        images.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterImages = await ReadTourAsync(images);

        var tooFew = await SendAsync(HttpMethod.Post, $"/catalog/manage/tours/{created.Id}/publish", null, afterImages.Etag);
        tooFew.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var missing = await ReadMissingAsync(tooFew);
        missing.Should().Contain(TourPublishRequirementCodes.ImagesMinCount);

        var third = Guid.NewGuid();
        var three = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/images",
            new UpdateTourImagesRequest(
            [
                new TourImageInput(cover, 0, true, "Обложка"),
                new TourImageInput(second, 1, false, "Второе"),
                new TourImageInput(third, 2, false, "Третье")
            ]),
            afterImages.Etag);
        three.StatusCode.Should().Be(HttpStatusCode.OK);
        var ready = await ReadTourAsync(three);

        _factory.Media.FailMark = true;
        var failedPublish = await SendAsync(HttpMethod.Post, $"/catalog/manage/tours/{created.Id}/publish", null, ready.Etag);
        failedPublish.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        StoredStatus(created.Id).Should().Be(TourStatus.Draft);
        _factory.Media.FailMark = false;

        var published = await SendAsync(HttpMethod.Post, $"/catalog/manage/tours/{created.Id}/publish", null, ready.Etag);
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        var live = await ReadTourAsync(published);
        live.Status.Should().Be(nameof(TourStatus.Published));
        _factory.Media.Marked.Should().Contain(new[] { cover, second, third });

        var broken = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/program",
            new UpdateTourProgramRequest([new TourDayInput(1, "Только один", "День")]),
            live.Etag);
        broken.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        StoredStatus(created.Id).Should().Be(TourStatus.Published);

        var deletePublished = await SendAsync(HttpMethod.Delete, $"/catalog/manage/tours/{created.Id}", null, live.Etag);
        deletePublished.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Images_WhenForeignFileOrUnknownWidthCover_Returns422()
    {
        var created = await CreateDraftAsync();
        var foreign = Guid.NewGuid();
        _factory.Media.Files = new Dictionary<Guid, RemoteMediaFile>
        {
            [foreign] = new(foreign, Guid.NewGuid().ToString(), 2000, 1000)
        };

        var foreignResponse = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/images",
            new UpdateTourImagesRequest([new TourImageInput(foreign, 0, false, "чужое")]),
            created.Etag);
        foreignResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var unknown = Guid.NewGuid();
        _factory.Media.Files = [];
        var unknownResponse = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/images",
            new UpdateTourImagesRequest([new TourImageInput(unknown, 0, false, "нет")]),
            created.Etag);
        unknownResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var cover = Guid.NewGuid();
        _factory.Media.Files = new Dictionary<Guid, RemoteMediaFile>
        {
            [cover] = new(cover, _managerId.ToString(), 0, 0)
        };
        var narrow = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/images",
            new UpdateTourImagesRequest([new TourImageInput(cover, 0, true, "обложка")]),
            created.Etag);
        narrow.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await narrow.Content.ReadAsStringAsync();
        body.Should().Contain(TourPublishRequirementCodes.ImagesCoverMinWidth);
    }

    [Fact]
    public async Task Images_WhenMediaIsDown_Returns503()
    {
        var created = await CreateDraftAsync();
        _factory.Media.FailGet = true;

        var response = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/images",
            new UpdateTourImagesRequest([new TourImageInput(Guid.NewGuid(), 0, false, "фото")]),
            created.Etag);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Manage_WhenAnotherManager_Returns403_AndAdminCanUnpublish()
    {
        var created = await CreateDraftAsync();
        Authorize(Guid.NewGuid(), "Manager");

        var foreign = await SendAsync(
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/basics",
            new UpdateTourBasicsRequest("Чужой", null, null, null, null, null, null),
            created.Etag);
        foreign.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var tour = CompletePublished();
        Authorize(Guid.NewGuid(), "Admin");
        var etag = TourEtagOf(tour);
        var response = await SendAsync(HttpMethod.Post, $"/catalog/manage/tours/{tour.Id}/unpublish", null, etag);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadTourAsync(response)).Status.Should().Be(nameof(TourStatus.Unpublished));
    }

    [Fact]
    public async Task Manage_WhenClient_Returns403_AndGuestReturns401()
    {
        Authorize(Guid.NewGuid(), "Client");
        var client = await _client.PostAsJsonAsync("/catalog/manage/tours", new { });
        client.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        _client.DefaultRequestHeaders.Authorization = null;
        var guest = await _client.GetAsync("/catalog/manage/tours");
        guest.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_WithoutIfMatch_Returns428_AndDraftWithMatch_Returns204()
    {
        var created = await CreateDraftAsync();
        var missing = await SendAsync(HttpMethod.Delete, $"/catalog/manage/tours/{created.Id}", null, null);
        missing.StatusCode.Should().Be(HttpStatusCode.PreconditionRequired);

        var deleted = await SendAsync(HttpMethod.Delete, $"/catalog/manage/tours/{created.Id}", null, created.Etag);
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task List_AdminSeesForeignTour_ManagerSeesOnlyOwn()
    {
        var own = await CreateDraftAsync();
        var foreignOwner = Guid.NewGuid();
        var foreign = Tour.CreateDraft(foreignOwner);
        foreign.SetBasics("Чужой тур", null, null, null, TourType.City, 3, null);
        _factory.UseDbContext(db =>
        {
            db.Tours.Add(foreign);
            db.SaveChanges();
        });

        var managerList = await _client.GetFromJsonAsync<List<TourManageDto>>("/catalog/manage/tours");
        managerList.Should().NotBeNull();
        var managerIds = managerList!.Select(t => t.Id).ToList();
        managerIds.Should().Contain(own.Id);
        managerIds.Should().NotContain(foreign.Id);

        Authorize(Guid.NewGuid(), "Admin");
        var adminList = await _client.GetFromJsonAsync<List<TourManageDto>>("/catalog/manage/tours");
        adminList.Should().NotBeNull();
        var adminIds = adminList!.Select(t => t.Id).ToList();
        adminIds.Should().Contain(own.Id);
        adminIds.Should().Contain(foreign.Id);
    }

    private async Task<TourManageDto> CreateDraftAsync()
    {
        var response = await _client.PostAsJsonAsync("/catalog/manage/tours", new CreateTourDraftRequest(
            null, null, null, null, null, null, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await ReadTourAsync(response);
    }

    private Tour CompletePublished()
    {
        var tour = Tour.CreateDraft(_managerId);
        tour.SetBasics("Админ снимет", "Кратко", "Москва", "Италия", TourType.City, 1, null);
        tour.SetDescription("Описание опубликованного тура");
        tour.ReplaceDays([TourDay.Create(tour.Id, 1, "День", "Программа")]);
        tour.ReplaceConditions(
            [TourInclusion.Create(tour.Id, "Завтрак", TourInclusionKind.Included)],
            MealPlan.BB,
            "Гостиница");
        var from = DateTime.UtcNow.AddDays(5);
        tour.ReplaceOffers([TourOffer.Create(tour.Id, from, from.AddDays(1), 100m, "EUR", 2)]);
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).ToArray();
        tour.ReplaceImages(
        [
            TourImage.Create(tour.Id, ids[0], 0, true, "Обложка", 1600),
            TourImage.Create(tour.Id, ids[1], 1, false, "Два", 800),
            TourImage.Create(tour.Id, ids[2], 2, false, "Три", 800)
        ], 1600);
        tour.Publish(DateTime.UtcNow, 1600);
        _factory.UseDbContext(db =>
        {
            db.Tours.Add(tour);
            db.SaveChanges();
        });
        return tour;
    }

    private string TourEtagOf(Tour tour)
    {
        string etag = "\"0\"";
        _factory.UseDbContext(db =>
        {
            var stored = db.Tours.Single(t => t.Id == tour.Id);
            etag = $"\"{stored.Version}\"";
        });
        return etag;
    }

    private TourStatus StoredStatus(Guid id)
    {
        TourStatus status = TourStatus.Draft;
        _factory.UseDbContext(db => status = db.Tours.Single(t => t.Id == id).Status);
        return status;
    }

    private void Authorize(Guid userId, string role)
    {
        var token = _factory.GenerateToken(userId.ToString(), role);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body, string? etag)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        if (!string.IsNullOrWhiteSpace(etag))
            request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await _client.SendAsync(request);
    }

    private static async Task<TourManageDto> ReadTourAsync(HttpResponseMessage response)
    {
        var tour = await response.Content.ReadFromJsonAsync<TourManageDto>();
        tour.Should().NotBeNull();
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag!.Tag.Should().Be(tour!.Etag);
        return tour;
    }

    private static async Task<List<string>> ReadMissingAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("missing").EnumerateArray().Select(item => item.GetString()!).ToList();
    }
}
