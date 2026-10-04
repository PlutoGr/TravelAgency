using Microsoft.Extensions.Configuration;
using TravelAgency.Media.API.Hosting;

namespace TravelAgency.Media.IntegrationTests.Hosting;

public class CleartextHostingTests
{
    [Fact]
    public void Http2Preface_FullSequence_IsComplete()
    {
        var preface = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n"u8.ToArray();

        Http2ConnectionPreface.IsPrefix(ReadOnlySpan<byte>.Empty).Should().BeTrue();
        Http2ConnectionPreface.IsPrefix(preface.AsSpan(0, 3)).Should().BeTrue();
        Http2ConnectionPreface.IsComplete(preface).Should().BeTrue();
        Http2ConnectionPreface.IsPrefix("GET /health/live"u8).Should().BeFalse();
        Http2ConnectionPreface.IsComplete(preface.AsSpan(0, 23)).Should().BeFalse();
    }

    [Fact]
    public void PublicUrls_PreferUrlsOverPorts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["urls"] = "http://127.0.0.1:5277",
                ["http_ports"] = "8080"
            })
            .Build();

        PublicEndpointUrls.Resolve(configuration).Should().Equal("http://127.0.0.1:5277");
    }

    [Fact]
    public void PublicUrls_ExpandHttpAndHttpsPorts()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["http_ports"] = "8080",
                ["https_ports"] = "8443"
            })
            .Build();

        PublicEndpointUrls.Resolve(configuration).Should().Equal("http://*:8080", "https://*:8443");
    }
}
