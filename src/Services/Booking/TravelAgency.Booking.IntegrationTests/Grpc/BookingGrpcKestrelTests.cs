using System.Net;
using Grpc.Core;
using Grpc.Net.Client;
using TravelAgency.Contracts.Grpc.Booking;

namespace TravelAgency.Booking.IntegrationTests.Grpc;

public sealed class BookingGrpcKestrelTests : IClassFixture<BookingGrpcKestrelFixture>
{
    private const string MethodPath = "/booking.BookingService/ValidateBookingAccess";

    private readonly BookingGrpcKestrelFixture _fixture;

    public BookingGrpcKestrelTests(BookingGrpcKestrelFixture fixture) => _fixture = fixture;

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
        using var channel = GrpcChannel.ForAddress(_fixture.Address);
        var client = new BookingService.BookingServiceClient(channel);
        var call = client.ValidateBookingAccessAsync(new ValidateBookingAccessRequest
        {
            BookingId = "x",
            UserId = "x"
        });

        var ex = await Assert.ThrowsAsync<RpcException>(async () => await call.ResponseAsync);
        ex.StatusCode.Should().Be(StatusCode.Internal);
        ex.Status.Detail.Should().Contain("HTTP_1_1_REQUIRED");
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
    public async Task ValidateBookingAccess_WithoutToken_ReturnsGrpcStatus16()
    {
        var (status, version) = await GrpcPortProbe.PostAsync($"{_fixture.GrpcAddress}{MethodPath}");

        version.Should().Be(HttpVersion.Version20);
        status.Should().Be("16");
    }

    [Fact]
    public async Task ValidateBookingAccess_WithServiceToken_DoesNotReturnGrpcStatus16()
    {
        var (status, version) = await GrpcPortProbe.PostAsync(
            $"{_fixture.GrpcAddress}{MethodPath}",
            BookingGrpcKestrelFixture.ServiceToken);

        version.Should().Be(HttpVersion.Version20);
        status.Should().NotBe("16");
        status.Should().Be("0");
    }
}
