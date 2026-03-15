using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Infrastructure.GrpcClients;
using TravelAgency.Contracts.Grpc.Catalog;

namespace TravelAgency.Booking.Infrastructure.UnitTests.GrpcClients;

public class CatalogGrpcClientTests
{
    [Fact]
    public async Task GetTourSnapshotAsync_WhenCatalogReturnsFound_ReturnsCorrectTourSnapshot()
    {
        var tourId = Guid.NewGuid();
        var snapshotTakenAt = DateTime.UtcNow.ToString("O");
        var expectedResponse = new TourSnapshotResponse
        {
            TourId = tourId.ToString(),
            Title = "Beach Paradise",
            Description = "Relax on pristine beaches",
            Price = 1999.99,
            Currency = "USD",
            DurationDays = 7,
            SnapshotTakenAt = snapshotTakenAt,
            Found = true
        };

        using var host = await CreateTestServerAsync(expectedResponse);
        var client = CreateCatalogGrpcClient(host);

        var result = await client.GetTourSnapshotAsync(tourId, CancellationToken.None);

        result.Should().NotBeNull();
        result.TourId.Should().Be(tourId);
        result.Title.Should().Be("Beach Paradise");
        result.Description.Should().Be("Relax on pristine beaches");
        result.Price.Should().Be(1999.99m);
        result.Currency.Should().Be("USD");
        result.DurationDays.Should().Be(7);
        result.SnapshotTakenAt.Should().Be(DateTime.Parse(snapshotTakenAt));
    }

    [Fact]
    public async Task GetTourSnapshotAsync_ProtoContractFields_AreCorrectlyMappedToBookingTourSnapshotDto()
    {
        var tourId = Guid.NewGuid();
        var snapshotTakenAt = "2025-03-14T12:00:00.0000000Z";
        var response = new TourSnapshotResponse
        {
            TourId = tourId.ToString(),
            Title = "Contract Test",
            Description = "Proto description field",
            Price = 999.50,
            Currency = "GBP",
            DurationDays = 3,
            SnapshotTakenAt = snapshotTakenAt,
            Found = true
        };

        using var host = await CreateTestServerAsync(response);
        var client = CreateCatalogGrpcClient(host);

        var result = await client.GetTourSnapshotAsync(tourId, CancellationToken.None);

        result.Description.Should().Be(response.Description, "description maps from proto");
        result.Price.Should().Be((decimal)response.Price, "price maps from proto");
        result.SnapshotTakenAt.Should().Be(DateTime.Parse(response.SnapshotTakenAt), "snapshot_taken_at maps from proto");
        result.TourId.Should().Be(tourId, "tour_id maps from proto");
    }

    [Fact]
    public async Task GetTourSnapshotAsync_WhenCatalogReturnsNotFound_ThrowsNotFoundException()
    {
        var tourId = Guid.NewGuid();
        var notFoundResponse = new TourSnapshotResponse { Found = false };

        using var host = await CreateTestServerAsync(notFoundResponse);
        var client = CreateCatalogGrpcClient(host);

        var act = async () => await client.GetTourSnapshotAsync(tourId, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Tour '{tourId}' was not found in catalog*");
    }

    private static async Task<IHost> CreateTestServerAsync(TourSnapshotResponse response)
    {
        var responseHolder = new ResponseHolder { Response = response };

        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddGrpc();
                    services.AddSingleton(responseHolder);
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<MockCatalogGrpcService>());
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static CatalogGrpcClient CreateCatalogGrpcClient(IHost host)
    {
        var server = host.GetTestServer();
        var httpClient = server.CreateClient();
        var baseAddress = httpClient.BaseAddress ?? new Uri("http://localhost");
        var channel = GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions
        {
            HttpClient = httpClient
        });
        var grpcClient = new CatalogService.CatalogServiceClient(channel);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrpcSettings:InternalServiceToken"] = "test-token"
            })
            .Build();
        return new CatalogGrpcClient(grpcClient, configuration);
    }

    private class ResponseHolder
    {
        public TourSnapshotResponse Response { get; set; } = null!;
    }

    private class MockCatalogGrpcService : CatalogService.CatalogServiceBase
    {
        private readonly ResponseHolder _holder;

        public MockCatalogGrpcService(ResponseHolder holder)
        {
            _holder = holder;
        }

        public override Task<TourSnapshotResponse> GetTourSnapshot(
            GetTourSnapshotRequest request,
            ServerCallContext context)
        {
            return Task.FromResult(_holder.Response);
        }
    }
}
