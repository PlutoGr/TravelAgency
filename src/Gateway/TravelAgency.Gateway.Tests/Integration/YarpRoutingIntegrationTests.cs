using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;
using TravelAgency.Gateway.Tests.Transforms;
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

    private HttpClient CreateClientForRole(string role)
    {
        var client = _fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", Helpers.JwtTokenHelper.GenerateToken(role: role));
        return client;
    }

    private static string GetRouteId(HttpResponseMessage response)
    {
        Assert.True(
            response.Headers.TryGetValues(RouteIdResponseTransformProvider.HeaderName, out var values),
            "Expected the test transform to report the selected YARP route id.");
        return values.Single();
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
        Assert.Equal("media-route", GetRouteId(response));
    }

    [Fact]
    public async Task Media_PublicFile_AnonymousGet_SelectsPublicRoute()
    {
        var client = CreateClient(withAuth: false);

        var response = await client.GetAsync("/api/v1/media/files/11111111-1111-1111-1111-111111111111/w800");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("media-files-public-route", GetRouteId(response));
        Assert.Equal("/media/files/11111111-1111-1111-1111-111111111111/w800", await GetEchoedPathAsync(response));
    }

    [Fact]
    public async Task Media_PublicFile_Post_DoesNotUseAnonymousRoute()
    {
        var client = CreateClientForRole(AppRoles.Manager);

        var response = await client.PostAsync(
            "/api/v1/media/files/11111111-1111-1111-1111-111111111111/w800",
            new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("media-route", GetRouteId(response));
    }

    [Fact]
    public async Task Media_ManageFile_ManagerGet_SelectsManageRoute()
    {
        var client = CreateClientForRole(AppRoles.Manager);

        var response = await client.GetAsync("/api/v1/media/manage/files/11111111-1111-1111-1111-111111111111/w200");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("media-manage-files-route", GetRouteId(response));
        Assert.Equal("/media/manage/files/11111111-1111-1111-1111-111111111111/w200", await GetEchoedPathAsync(response));
    }

    [Fact]
    public async Task Media_ManageFile_AdminGet_SelectsManageRoute()
    {
        var client = CreateClientForRole(AppRoles.Admin);

        var response = await client.GetAsync("/api/v1/media/manage/files/11111111-1111-1111-1111-111111111111/w1600");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("media-manage-files-route", GetRouteId(response));
    }

    [Fact]
    public async Task Media_ManageFile_ClientGet_Returns403()
    {
        var client = CreateClientForRole(AppRoles.Client);

        var response = await client.GetAsync("/api/v1/media/manage/files/11111111-1111-1111-1111-111111111111/w200");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Media_ManageFile_GuestGet_Returns401()
    {
        var client = CreateClient(withAuth: false);

        var response = await client.GetAsync("/api/v1/media/manage/files/11111111-1111-1111-1111-111111111111/w200");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Media_Upload_GuestPost_Returns401()
    {
        var client = CreateClient(withAuth: false);

        var response = await client.PostAsync("/api/v1/media/upload?purpose=tour-image", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Media_GrpcPath_IsNotRoutedThroughGateway()
    {
        var client = CreateClient(withAuth: false);

        var root = await client.PostAsync("/media.MediaService/GetMediaFiles", new StringContent(string.Empty));
        var underApi = await client.PostAsync("/api/v1/media.MediaService/MarkMediaFilesPublic", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.NotFound, root.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, underApi.StatusCode);
        Assert.False(root.Headers.Contains(RouteIdResponseTransformProvider.HeaderName));
        Assert.False(underApi.Headers.Contains(RouteIdResponseTransformProvider.HeaderName));
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
