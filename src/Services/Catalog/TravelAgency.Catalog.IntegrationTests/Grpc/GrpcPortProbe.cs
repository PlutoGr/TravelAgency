using System.Net;
using System.Net.Http.Headers;

namespace TravelAgency.Catalog.IntegrationTests.Grpc;

internal static class GrpcPortProbe
{
    public static Task<(string? Status, Version Version)> PostAsync(string url, string? token = null) =>
        PostAsync(url, EmptyGrpcFrame, token);

    public static async Task<(string? Status, Version Version)> PostAsync(string url, byte[] body, string? token)
    {
        using var handler = new SocketsHttpHandler { EnableMultipleHttp2Connections = true };
        using var http = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
            Content = new ByteArrayContent(body)
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

    private static readonly byte[] EmptyGrpcFrame = [0, 0, 0, 0, 0];
}
