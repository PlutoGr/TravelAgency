using Grpc.Net.Client;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Contracts.Grpc.Catalog;

namespace TravelAgency.Catalog.IntegrationTests.GrpcServices;

[Collection(nameof(CatalogIntegrationTestCollection))]
public class CatalogGrpcServiceTests
{
    private readonly CatalogTestFixture _fixture;

    public CatalogGrpcServiceTests(CatalogTestFixture fixture)
    {
        _fixture = fixture;
    }

    private CatalogService.CatalogServiceClient CreateGrpcClient()
    {
        var httpClient = _fixture.Factory.CreateClient();
        var baseAddress = httpClient.BaseAddress ?? new Uri("http://localhost");
        var channel = GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions { HttpClient = httpClient });
        return new CatalogService.CatalogServiceClient(channel);
    }

    [Fact]
    public async Task GetTourSnapshot_WithExistingTour_ReturnsCorrectResponseShape()
    {
        var tour = Tour.Create("Grpc Test Tour", "Beach vacation description", TourType.Beach, "Spain", 5, null, null);
        var validFrom = DateTime.UtcNow.AddDays(-1);
        var validTo = DateTime.UtcNow.AddDays(30);
        var price = TourPrice.Create(tour.Id, validFrom, validTo, 1500m, "EUR", 15);
        tour.SetPrices([price]);

        _fixture.Factory.UseDbContext(db =>
        {
            db.Tours.Add(tour);
            db.SaveChanges();
        });

        var client = CreateGrpcClient();

        var response = await client.GetTourSnapshotAsync(new GetTourSnapshotRequest { TourId = tour.Id.ToString() });

        response.Should().NotBeNull();
        response.Found.Should().BeTrue();
        response.TourId.Should().Be(tour.Id.ToString());
        response.Title.Should().Be("Grpc Test Tour");
        response.Description.Should().Be("Beach vacation description");
        response.Price.Should().Be(1500.0);
        response.Currency.Should().Be("EUR");
        response.DurationDays.Should().Be(5);
        response.SnapshotTakenAt.Should().NotBeNullOrEmpty();
        DateTime.TryParse(response.SnapshotTakenAt, out _).Should().BeTrue("snapshot_taken_at should be valid ISO8601");
    }

    [Fact]
    public async Task GetTourSnapshot_WithNonExistingTour_ReturnsFoundFalse()
    {
        var client = CreateGrpcClient();

        var response = await client.GetTourSnapshotAsync(new GetTourSnapshotRequest { TourId = Guid.NewGuid().ToString() });

        response.Should().NotBeNull();
        response.Found.Should().BeFalse();
    }

    [Fact]
    public async Task GetTourSnapshot_WithInvalidTourId_ReturnsFoundFalse()
    {
        var client = CreateGrpcClient();

        var response = await client.GetTourSnapshotAsync(new GetTourSnapshotRequest { TourId = "not-a-valid-guid" });

        response.Should().NotBeNull();
        response.Found.Should().BeFalse();
    }
}
