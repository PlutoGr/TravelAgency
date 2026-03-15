using Xunit;

namespace TravelAgency.Gateway.Tests.Integration;

/// <summary>
/// Fixture for auth response transform integration tests. Starts a mock backend that
/// returns token JSON for /identity/login, /identity/register, /identity/refresh.
/// </summary>
public class AuthResponseTransformFixture : IAsyncLifetime
{
    public Helpers.MockBackendServer MockServer { get; private set; } = null!;
    public Helpers.GatewayWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        MockServer = await Helpers.MockBackendServer.StartAsync(
            authTokenSupport: true,
            validJwtFactory: () => Helpers.JwtTokenHelper.GenerateToken());
        Factory = new Helpers.GatewayWebApplicationFactory(MockServer.BaseUrl);
    }

    public async Task DisposeAsync()
    {
        await MockServer.DisposeAsync();
        await Factory.DisposeAsync();
    }
}
