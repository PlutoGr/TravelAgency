using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace TravelAgency.Shared.Infrastructure.Hosting;

/// <summary>
/// Resolves the public listen URLs the generic host would have given Kestrel:
/// <c>urls</c> / <c>ASPNETCORE_URLS</c> win, otherwise <c>ASPNETCORE_HTTP_PORTS</c>
/// and <c>ASPNETCORE_HTTPS_PORTS</c> expand to <c>http(s)://*:port</c>.
/// </summary>
public static class PublicEndpointUrls
{
    public static IReadOnlyList<string> Resolve(IConfiguration configuration)
    {
        var urls = configuration[WebHostDefaults.ServerUrlsKey];
        if (string.IsNullOrWhiteSpace(urls))
        {
            var http = Expand(configuration[WebHostDefaults.HttpPortsKey], Uri.UriSchemeHttp);
            var https = Expand(configuration[WebHostDefaults.HttpsPortsKey], Uri.UriSchemeHttps);
            urls = string.Join(';', new[] { http, https }.Where(static value => value.Length > 0));
        }

        if (string.IsNullOrWhiteSpace(urls))
            return [];

        return urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string Expand(string? ports, string scheme)
    {
        if (string.IsNullOrWhiteSpace(ports))
            return string.Empty;

        return string.Join(
            ';',
            ports.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(port => $"{scheme}://*:{port}"));
    }
}
