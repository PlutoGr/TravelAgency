using System.Net;
using System.Net.Http.Headers;

namespace TravelAgency.Booking.IntegrationTests.Grpc;

internal static class GrpcPortProbe
{
    public static async Task<(string? Status, Version Version)> PostAsync(string url, string? token = null)
    {
        using var handler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true };
        using var http = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new ByteArrayContent([0, 0, 0, 0, 0])
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/grpc");
        request.Headers.TryAddWithoutValidation("TE", "trailers");
        if (token is not null)
            request.Headers.TryAddWithoutValidation("x-internal-auth", token);

        using var response = await http.SendAsync(request);
        var version = response.Version;
        await response.Content.CopyToAsync(Stream.Null);
        var status = Read(response.Headers) ?? Read(response.TrailingHeaders);
        return (status, version);
    }

    private static string? Read(HttpHeaders headers) =>
        headers.TryGetValues("grpc-status", out var values) ? values.FirstOrDefault() : null;
}
