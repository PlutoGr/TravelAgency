using System.Net;
using System.Net.Http.Json;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.IntegrationTests.Helpers;

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
        var published = PublishedTourSeed.Create(
            "Unique Tour For Paged Test", "Visible description", 2, 1000m, "EUR", 4);
        _factory.UseDbContext(db =>
        {
            db.Tours.Add(published);
            db.SaveChanges();
        });

        var response = await _client.GetAsync("/catalog/tours");
        var result = await response.Content.ReadFromJsonAsync<PagedResult<TourSummaryDto>>();

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTourById_WithPublishedTour_ReturnsOkAndEmptyComponents()
    {
        var tour = PublishedTourSeed.Create("Tour For GetById", "Visible description", 2, 1000m, "EUR", 4);
        _factory.UseDbContext(db =>
        {
            db.Tours.Add(tour);
            db.SaveChanges();
        });

        var response = await _client.GetAsync($"/catalog/tours/{tour.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PublicTourDto>();
        result!.Id.Should().Be(tour.Id);
        result.Components.Should().BeEmpty();
        result.Days.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTourById_DraftAndUnpublished_Return404()
    {
        var draft = SeedTour("Hidden Draft");
        var draftResponse = await _client.GetAsync($"/catalog/tours/{draft.Id}");
        draftResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var hidden = PublishedTourSeed.Create("Hidden Unpublished", "Gone", 2, 800m, "EUR", 2);
        hidden.Unpublish();
        _factory.UseDbContext(db =>
        {
            db.Tours.Add(hidden);
            db.SaveChanges();
        });

        var hiddenResponse = await _client.GetAsync($"/catalog/tours/{hidden.Id}");
        hiddenResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTourById_WithNonExistingTour_Returns404()
    {
        var response = await _client.GetAsync($"/catalog/tours/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("POST", "/catalog/tours")]
    [InlineData("PUT", "/catalog/tours/11111111-1111-1111-1111-111111111111")]
    [InlineData("PATCH", "/catalog/tours/11111111-1111-1111-1111-111111111111/prices")]
    [InlineData("DELETE", "/catalog/tours/11111111-1111-1111-1111-111111111111")]
    public async Task LegacyWriteEndpoints_AreRemoved(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT" or "PATCH")
            request.Content = JsonContent.Create(new { title = "legacy" });

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.MethodNotAllowed, HttpStatusCode.NotFound);
    }
}
