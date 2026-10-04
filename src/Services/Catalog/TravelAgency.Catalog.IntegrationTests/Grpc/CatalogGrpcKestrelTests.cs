using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using TravelAgency.Contracts.Grpc.Catalog;

namespace TravelAgency.Catalog.IntegrationTests.Grpc;

public sealed class CatalogGrpcKestrelTests : IClassFixture<CatalogGrpcKestrelFixture>
{
    private const string MethodPath = "/catalog.CatalogService/GetTourSnapshot";

    private readonly CatalogGrpcKestrelFixture _fixture;

    public CatalogGrpcKestrelTests(CatalogGrpcKestrelFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task RestPort_ServesHttp1Health()
    {
        using var http = new HttpClient();
        var live = await http.GetAsync($"{_fixture.Address}/health/live");

        live.StatusCode.Should().Be(HttpStatusCode.OK);
        live.Version.Should().Be(HttpVersion.Version11);
    }

    [Fact]
    public async Task Grpc_OnRestPort_IsRefused()
    {
        new Uri(_fixture.Address).Host.Should().Be(new Uri(_fixture.GrpcAddress).Host);

        using var liveChannel = GrpcChannel.ForAddress(_fixture.GrpcAddress);
        var liveClient = new CatalogService.CatalogServiceClient(liveChannel);
        var snapshot = await liveClient.GetTourSnapshotAsync(
            new GetTourSnapshotRequest { TourId = "x" },
            new Metadata { { "x-internal-auth", CatalogGrpcKestrelFixture.ServiceToken } });
        snapshot.Found.Should().BeFalse();

        using var channel = GrpcChannel.ForAddress(_fixture.Address);
        var client = new CatalogService.CatalogServiceClient(channel);
        var call = client.GetTourSnapshotAsync(new GetTourSnapshotRequest { TourId = "x" });

        var ex = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);
        ex.StatusCode.Should().BeOneOf(StatusCode.Unavailable, StatusCode.Internal);
    }

    [Fact]
    public async Task Rest_OnGrpcPort_IsNotServed()
    {
        using var handler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true };
        using var http = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_fixture.GrpcAddress}/health/live")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        var response = await http.SendAsync(request);

        response.Version.Should().Be(HttpVersion.Version20);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTourSnapshot_WithoutToken_ReturnsGrpcStatus16()
    {
        var (status, version) = await GrpcPortProbe.PostAsync($"{_fixture.GrpcAddress}{MethodPath}");

        version.Should().Be(HttpVersion.Version20);
        status.Should().Be("16");
    }

    [Fact]
    public async Task GetTourSnapshot_WithServiceToken_DoesNotReturnGrpcStatus16()
    {
        var (status, version) = await GrpcPortProbe.PostAsync(
            $"{_fixture.GrpcAddress}{MethodPath}",
            CatalogGrpcKestrelFixture.ServiceToken);

        version.Should().Be(HttpVersion.Version20);
        status.Should().NotBe("16");
        status.Should().Be("0");
    }
}
