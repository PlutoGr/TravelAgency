using Grpc.Core;
using Grpc.Net.Client;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.IntegrationTests.Helpers;
using TravelAgency.Contracts.Grpc.Catalog;

namespace TravelAgency.Catalog.IntegrationTests.GrpcServices;

[Collection(nameof(CatalogIntegrationTestCollection))]
public class CatalogGrpcServiceTests
{
    private const string TestGrpcToken = "test-internal-token";

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

    private static CallOptions CreateCallOptions(CancellationToken ct = default)
    {
        var metadata = new Metadata { { "x-internal-auth", TestGrpcToken } };
        return new CallOptions(metadata, deadline: null, ct);
    }

    [Fact]
    public async Task GetTourSnapshot_WithExistingTour_ReturnsCorrectResponseShape()
    {
        var tour = PublishedTourSeed.Create(
            "Grpc Test Tour", "Beach vacation description", 5, 1500m, "EUR", 15);

        _fixture.Factory.UseDbContext(db =>
        {
            db.Tours.Add(tour);
            db.SaveChanges();
        });

        var client = CreateGrpcClient();

        var response = await client.GetTourSnapshotAsync(
            new GetTourSnapshotRequest { TourId = tour.Id.ToString() },
            CreateCallOptions());

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

        var response = await client.GetTourSnapshotAsync(
            new GetTourSnapshotRequest { TourId = Guid.NewGuid().ToString() },
            CreateCallOptions());

        response.Should().NotBeNull();
        response.Found.Should().BeFalse();
    }

    [Fact]
    public async Task GetTourSnapshot_WithInvalidTourId_ReturnsFoundFalse()
    {
        var client = CreateGrpcClient();

        var response = await client.GetTourSnapshotAsync(
            new GetTourSnapshotRequest { TourId = "not-a-valid-guid" },
            CreateCallOptions());

        response.Should().NotBeNull();
        response.Found.Should().BeFalse();
    }

    [Fact]
    public async Task GetTourSnapshot_WithoutAuthHeader_ReturnsUnauthenticated()
    {
        var client = CreateGrpcClient();
        var call = client.GetTourSnapshotAsync(
            new GetTourSnapshotRequest { TourId = Guid.NewGuid().ToString() },
            new CallOptions(cancellationToken: CancellationToken.None));

        Func<Task> act = async () => await call.ResponseAsync;
        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }

    [Fact]
    public async Task GetTourSnapshot_WithInvalidToken_ReturnsUnauthenticated()
    {
        var client = CreateGrpcClient();
        var invalidMetadata = new Metadata { { "x-internal-auth", "wrong-token" } };
        var callOptions = new CallOptions(invalidMetadata, deadline: null, CancellationToken.None);
        var call = client.GetTourSnapshotAsync(
            new GetTourSnapshotRequest { TourId = Guid.NewGuid().ToString() },
            callOptions);

        Func<Task> act = async () => await call.ResponseAsync;
        var ex = await act.Should().ThrowAsync<RpcException>();
        ex.Which.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }

    [Fact]
    public async Task GetTourSnapshotForExistingBooking_WhenUnpublished_ReturnsSnapshot_AndNewBookingSnapshotDoesNot()
    {
        var tour = PublishedTourSeed.Create("Снятый тур", "Описание снятого тура", 4, 2200m, "RUB", 6);
        tour.Unpublish();
        _fixture.Factory.UseDbContext(db =>
        {
            db.Tours.Add(tour);
            db.SaveChanges();
        });

        var client = CreateGrpcClient();
        var hidden = await client.GetTourSnapshotAsync(
            new GetTourSnapshotRequest { TourId = tour.Id.ToString() },
            CreateCallOptions());
        hidden.Found.Should().BeFalse();

        var existing = await client.GetTourSnapshotForExistingBookingAsync(
            new GetTourSnapshotRequest { TourId = tour.Id.ToString() },
            CreateCallOptions());

        existing.Found.Should().BeTrue();
        existing.TourId.Should().Be(tour.Id.ToString());
        existing.Title.Should().Be("Снятый тур");
        existing.Description.Should().Be("Описание снятого тура");
        existing.Price.Should().Be(2200.0);
        existing.Currency.Should().Be("RUB");
        existing.DurationDays.Should().Be(4);
        existing.SnapshotTakenAt.Should().EndWith("Z");
    }

    [Fact]
    public async Task GetTourSnapshotForExistingBooking_WhenDraft_ReturnsFoundFalse()
    {
        var tour = Tour.Create("Черновик", "Ещё не публиковали", TourType.City, "Италия", 3, null);
        _fixture.Factory.UseDbContext(db =>
        {
            db.Tours.Add(tour);
            db.SaveChanges();
        });

        var client = CreateGrpcClient();
        var response = await client.GetTourSnapshotForExistingBookingAsync(
            new GetTourSnapshotRequest { TourId = tour.Id.ToString() },
            CreateCallOptions());

        response.Found.Should().BeFalse();
    }
}
