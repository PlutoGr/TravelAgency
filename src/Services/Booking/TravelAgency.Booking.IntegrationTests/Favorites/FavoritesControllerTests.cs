using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.IntegrationTests.Helpers;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.IntegrationTests.Favorites;

public class FavoritesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly Guid _clientId = Guid.NewGuid();

    public FavoritesControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.EnsureDbCreated();
    }

    private void AuthorizeAsClient() =>
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(_clientId, AppRoles.Client));

    private void ClearAuthorization() =>
        _client.DefaultRequestHeaders.Authorization = null;

    [Fact]
    public async Task GetFavorites_AsClient_ShouldReturn200WithList()
    {
        AuthorizeAsClient();

        var response = await _client.GetAsync("/favorites");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<FavoriteDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFavorites_Unauthenticated_ShouldReturn401()
    {
        ClearAuthorization();

        var response = await _client.GetAsync("/favorites");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddFavorite_AsClient_ShouldReturn201()
    {
        AuthorizeAsClient();
        var tourId = Guid.NewGuid();

        var response = await _client.PostAsync($"/favorites/{tourId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<FavoriteDto>();
        result.Should().NotBeNull();
        result!.TourId.Should().Be(tourId);
        result.UserId.Should().Be(_clientId);
    }

    [Fact]
    public async Task AddFavorite_Duplicate_ShouldReturn409()
    {
        AuthorizeAsClient();
        var tourId = Guid.NewGuid();

        await _client.PostAsync($"/favorites/{tourId}", null);
        var response = await _client.PostAsync($"/favorites/{tourId}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Conflict");
    }

    [Fact]
    public async Task AddFavorite_Unauthenticated_ShouldReturn401()
    {
        ClearAuthorization();

        var response = await _client.PostAsync($"/favorites/{Guid.NewGuid()}", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveFavorite_AsClient_AfterAdding_ShouldReturn204()
    {
        AuthorizeAsClient();
        var tourId = Guid.NewGuid();

        await _client.PostAsync($"/favorites/{tourId}", null);
        var response = await _client.DeleteAsync($"/favorites/{tourId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveFavorite_NonExistent_ShouldReturn404()
    {
        AuthorizeAsClient();

        var response = await _client.DeleteAsync($"/favorites/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Not Found");
    }

    [Fact]
    public async Task RemoveFavorite_Unauthenticated_ShouldReturn401()
    {
        ClearAuthorization();

        var response = await _client.DeleteAsync($"/favorites/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFavorites_AfterAddingAndRemoving_ShouldReturnOnlyRemainingFavorites()
    {
        AuthorizeAsClient();
        var tourIdKept = Guid.NewGuid();
        var tourIdRemoved = Guid.NewGuid();

        await _client.PostAsync($"/favorites/{tourIdKept}", null);
        await _client.PostAsync($"/favorites/{tourIdRemoved}", null);
        await _client.DeleteAsync($"/favorites/{tourIdRemoved}");

        var response = await _client.GetAsync("/favorites");
        var result = await response.Content.ReadFromJsonAsync<List<FavoriteDto>>();

        result.Should().NotBeNull();
        result!.Should().Contain(f => f.TourId == tourIdKept);
        result.Should().NotContain(f => f.TourId == tourIdRemoved);
    }
}
