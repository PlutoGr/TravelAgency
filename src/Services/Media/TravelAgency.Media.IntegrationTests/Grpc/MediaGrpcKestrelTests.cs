using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using TravelAgency.Contracts.Grpc.Media;
using TravelAgency.Shared.Infrastructure.Hosting;
using TravelAgency.Media.Domain;

namespace TravelAgency.Media.IntegrationTests.Grpc;

public sealed class MediaGrpcKestrelTests : IClassFixture<MediaGrpcKestrelFixture>
{
    private readonly MediaGrpcKestrelFixture _fixture;

    public MediaGrpcKestrelTests(MediaGrpcKestrelFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RestPort_ServesHttp1Health_AndGrpcPort_ServesHttp2()
    {
        using var http = new HttpClient();
        var live = await http.GetAsync($"{_fixture.Address}/health/live");
        live.StatusCode.Should().Be(HttpStatusCode.OK);
        live.Version.Should().Be(HttpVersion.Version11);

        var file = await _fixture.SeedTourImageAsync();
        var (client, recorder) = CreateClient();

        var response = await client.GetMediaFilesAsync(
            new GetMediaFilesRequest { Ids = { file.Id.ToString() } },
            AuthHeaders());

        recorder.RequestVersion.Should().Be(HttpVersion.Version20);
        recorder.ResponseVersion.Should().Be(HttpVersion.Version20);
        response.Files.Should().ContainSingle();
        response.Files[0].Id.Should().Be(file.Id.ToString());
        response.Files[0].OwnerId.Should().Be(file.OwnerId);
        response.Files[0].ContentType.Should().Be("image/jpeg");
        response.Files[0].Width.Should().Be(2400);
        response.Files[0].Height.Should().Be(1350);
        response.Files[0].IsPublic.Should().BeFalse();
        response.Files[0].AvailableSizes.Should().Equal(
            TourImageSizes.W200, TourImageSizes.W800, TourImageSizes.W1600);
    }

    [Fact]
    public async Task MarkMediaFilesPublic_IsIdempotent_AndNeverRequiredToHide()
    {
        var file = await _fixture.SeedTourImageAsync();
        var (client, _) = CreateClient();
        var request = new MarkMediaFilesPublicRequest { Ids = { file.Id.ToString(), "not-a-guid" } };

        await client.MarkMediaFilesPublicAsync(request, AuthHeaders());
        await client.MarkMediaFilesPublicAsync(request, AuthHeaders());

        var listed = await client.GetMediaFilesAsync(
            new GetMediaFilesRequest { Ids = { file.Id.ToString(), Guid.NewGuid().ToString() } },
            AuthHeaders());

        listed.Files.Should().ContainSingle();
        listed.Files[0].IsPublic.Should().BeTrue();
    }

    [Fact]
    public async Task Grpc_OnRestPort_IsRefused()
    {
        var (client, _) = CreateClient(_fixture.Address);
        var call = client.GetMediaFilesAsync(
            new GetMediaFilesRequest { Ids = { Guid.NewGuid().ToString() } },
            AuthHeaders());

        var ex = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);
        ex.StatusCode.Should().Be(StatusCode.Internal);
        ex.Status.Detail.Should().Contain("HTTP_1_1_REQUIRED");
    }

    [Fact]
    public async Task Rest_OnGrpcPort_IsNotServed()
    {
        using var handler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true };
        using var http = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://127.0.0.1:{ServiceListenSettings.DefaultGrpcPort}/health/live")
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact
        };

        var response = await http.SendAsync(request);

        response.Version.Should().Be(HttpVersion.Version20);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMediaFiles_WithoutToken_IsUnauthenticated()
    {
        var (client, _) = CreateClient();
        var call = client.GetMediaFilesAsync(new GetMediaFilesRequest { Ids = { Guid.NewGuid().ToString() } });

        var ex = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);
        ex.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }

    [Fact]
    public async Task MarkMediaFilesPublic_WithWrongToken_IsUnauthenticated()
    {
        var (client, _) = CreateClient();
        var headers = new Metadata { { "x-internal-auth", "wrong-token" } };
        var call = client.MarkMediaFilesPublicAsync(new MarkMediaFilesPublicRequest(), headers);

        var ex = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);
        ex.StatusCode.Should().Be(StatusCode.Unauthenticated);
    }

    private (MediaService.MediaServiceClient Client, VersionRecordingHandler Recorder) CreateClient(string? address = null)
    {
        var recorder = new VersionRecordingHandler();
        var channel = GrpcChannel.ForAddress(address ?? _fixture.GrpcAddress, new GrpcChannelOptions
        {
            HttpHandler = recorder
        });
        return (new MediaService.MediaServiceClient(channel), recorder);
    }

    private static Metadata AuthHeaders() =>
        new() { { "x-internal-auth", MediaGrpcKestrelFixture.ServiceToken } };

    private sealed class VersionRecordingHandler : DelegatingHandler
    {
        public Version? RequestVersion { get; private set; }
        public Version? ResponseVersion { get; private set; }

        public VersionRecordingHandler()
            : base(new SocketsHttpHandler { EnableMultipleHttp2Connections = true })
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestVersion = request.Version;
            var response = await base.SendAsync(request, cancellationToken);
            ResponseVersion = response.Version;
            return response;
        }
    }
}
