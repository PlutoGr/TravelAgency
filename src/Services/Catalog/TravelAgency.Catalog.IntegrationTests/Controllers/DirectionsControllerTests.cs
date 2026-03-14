using System.Net;
using System.Net.Http.Json;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.IntegrationTests.Controllers;

[Collection(nameof(CatalogIntegrationTestCollection))]
public class DirectionsControllerTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DirectionsControllerTests(CatalogTestFixture fixture)
    {
        _factory = fixture.Factory;
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task GetDirections_ReturnsOk()
    {
        var response = await _client.GetAsync("/catalog/directions");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDirections_WithSeededData_ReturnsDirections()
    {
        var direction = Direction.Create("Greece", "Greece", "Mediterranean country");
        _factory.UseDbContext(db =>
        {
            db.Directions.Add(direction);
            db.SaveChanges();
        });

        var response = await _client.GetAsync("/catalog/directions");
        var result = await response.Content.ReadFromJsonAsync<List<DirectionDto>>();

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
    }
}
