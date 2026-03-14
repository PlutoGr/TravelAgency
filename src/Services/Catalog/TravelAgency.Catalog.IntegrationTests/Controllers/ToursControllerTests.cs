using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.IntegrationTests.Controllers;

[Collection(nameof(CatalogIntegrationTestCollection))]
public class ToursControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ToursControllerTests(CatalogTestFixture fixture)
    {
        _factory = fixture.Factory;
        _client = fixture.Factory.CreateClient();
    }

    private void AuthorizeAsManager()
    {
        var token = _factory.GenerateToken(Guid.NewGuid().ToString(), "Manager");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private Tour SeedTour(string title = "Test Tour")
    {
        var tour = Tour.Create(title, "Test Description", TourType.Beach, "Greece", 7, null, null);
        _factory.UseDbContext(db =>
        {
            db.Tours.Add(tour);
            db.SaveChanges();
        });
        return tour;
    }

    [Fact]
    public async Task GetTours_ReturnsOk()
    {
        var response = await _client.GetAsync("/catalog/tours");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTours_WithData_ReturnsPagedResult()
    {
        SeedTour("Unique Tour For Paged Test");

        var response = await _client.GetAsync("/catalog/tours");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TourSummaryDto>>();

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTourById_WithExistingTour_ReturnsOk()
    {
        var tour = SeedTour("Tour For GetById");

        var response = await _client.GetAsync($"/catalog/tours/{tour.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TourDto>();
        result!.Id.Should().Be(tour.Id);
    }

    [Fact]
    public async Task GetTourById_WithNonExistingTour_Returns404()
    {
        var response = await _client.GetAsync($"/catalog/tours/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateTour_WithManagerToken_Returns201()
    {
        AuthorizeAsManager();
        var request = new CreateTourRequest("New Integration Tour", "Description", TourType.City, "France", 5, null, null);

        var response = await _client.PostAsJsonAsync("/catalog/tours", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TourDto>();
        result!.Title.Should().Be("New Integration Tour");
    }

    [Fact]
    public async Task CreateTour_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var request = new CreateTourRequest("Unauthorized Tour", "Desc", TourType.City, "France", 5, null, null);

        var response = await _client.PostAsJsonAsync("/catalog/tours", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateTour_WithClientToken_Returns403()
    {
        var token = _factory.GenerateToken(Guid.NewGuid().ToString(), "Client");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var request = new CreateTourRequest("Client Tour", "Desc", TourType.City, "France", 5, null, null);

        var response = await _client.PostAsJsonAsync("/catalog/tours", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateTour_WithManagerToken_ReturnsOk()
    {
        AuthorizeAsManager();
        var tour = SeedTour("Tour To Update");
        var request = new UpdateTourRequest("Updated Title", "New Desc", TourType.Mountain, "Italy", 10, null, null);

        var response = await _client.PutAsJsonAsync($"/catalog/tours/{tour.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TourDto>();
        result!.Title.Should().Be("Updated Title");
    }

    [Fact]
    public async Task UpdateTour_WithNonExistingId_Returns404()
    {
        AuthorizeAsManager();
        var request = new UpdateTourRequest("Title", "Desc", TourType.Beach, "Greece", 7, null, null);

        var response = await _client.PutAsJsonAsync($"/catalog/tours/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateTourPrices_WithManagerToken_ReturnsOk()
    {
        AuthorizeAsManager();
        var tour = SeedTour("Tour For Prices Update");
        var request = new UpdateTourPricesRequest(
        [
            new TourPriceRequest(
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(30),
                1500m,
                "USD",
                10)
        ]);

        var response = await _client.PatchAsJsonAsync($"/catalog/tours/{tour.Id}/prices", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
