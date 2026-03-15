namespace TravelAgency.Gateway.Tests.Integration;

/// <summary>
/// Shared fixture for YARP routing tests. Starts a mock backend and creates a
/// Gateway WebApplicationFactory configured to proxy to it.
/// </summary>
public class YarpRoutingFixture : IAsyncLifetime, IAsyncDisposable
{
    public Helpers.MockBackendServer MockServer { get; private set; } = null!;
    public Helpers.GatewayWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        MockServer = await Helpers.MockBackendServer.StartAsync();
        Factory = new Helpers.GatewayWebApplicationFactory(MockServer.BaseUrl);
    }

    public async ValueTask DisposeAsync()
    {
        await MockServer.DisposeAsync();
        await Factory.DisposeAsync();
    }
}
