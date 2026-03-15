using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Gateway.Tests.Integration;

/// <summary>
/// Verifies that YARP correctly routes and transforms paths for anonymous and
/// authenticated routes. Uses a mock HTTP backend that echoes the received path.
/// </summary>
public class YarpRoutingIntegrationTests : IClassFixture<YarpRoutingFixture>
{
    private readonly YarpRoutingFixture _fixture;

    public YarpRoutingIntegrationTests(YarpRoutingFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient CreateClient(bool withAuth = false)
    {
        var client = _fixture.Factory.CreateClient();
        if (withAuth)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client));
        }
        return client;
    }

    private static async Task<string> GetEchoedPathAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("path").GetString() ?? string.Empty;
    }

    [Fact]
    public async Task Auth_GetMe_ProxiesToIdentityMe()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/identity/me", path);
    }

    [Fact]
    public async Task Auth_PostLogin_ProxiesToIdentityLogin()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "test@test.com", password = "x" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/identity/login", path);
    }

    [Fact]
    public async Task Catalog_GetTours_ProxiesToCatalogTours()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.GetAsync("/api/v1/catalog/tours");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/catalog/tours", path);
    }

    [Fact]
    public async Task Bookings_GetMy_WithJwt_ProxiesToBookingsMy()
    {
        // Arrange
        var client = CreateClient(withAuth: true);

        // Act
        var response = await client.GetAsync("/api/v1/bookings/my");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/bookings/my", path);
    }

    [Fact]
    public async Task Bookings_GetMy_WithoutJwt_Returns401()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.GetAsync("/api/v1/bookings/my");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Favorites_Get_WithJwt_ProxiesToFavorites()
    {
        // Arrange
        var client = CreateClient(withAuth: true);

        // Act
        var response = await client.GetAsync("/api/v1/favorites");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/favorites", path);
    }

    [Fact]
    public async Task Chat_GetHubs_WithJwt_ProxiesToChatHubs()
    {
        // Arrange
        var client = CreateClient(withAuth: true);

        // Act
        var response = await client.GetAsync("/api/v1/chat/hubs");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/chat/hubs", path);
    }

    [Fact]
    public async Task Media_GetPresign_WithJwt_ProxiesToMediaPresign()
    {
        // Arrange
        var client = CreateClient(withAuth: true);

        // Act
        var response = await client.GetAsync("/api/v1/media/presign");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/media/presign", path);
    }

    [Fact]
    public async Task HealthReady_Returns200_WithMockBackend()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
