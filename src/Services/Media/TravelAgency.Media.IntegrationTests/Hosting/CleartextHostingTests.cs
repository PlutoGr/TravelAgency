using Microsoft.Extensions.Configuration;
using TravelAgency.Media.API.Hosting;

namespace TravelAgency.Media.IntegrationTests.Hosting;

public class CleartextHostingTests
{
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
