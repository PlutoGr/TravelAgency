using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.IntegrationTests.MediaController;

public class DeleteTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public DeleteTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<MediaFile> SeedMediaFileAsync(string ownerId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")
    {
        _factory.EnsureDatabaseCreated();
        var file = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "storage/key/photo.jpg", ownerId);

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
        await repo.AddAsync(file);
        await repo.SaveChangesAsync();

        return file;
    }

    [Fact]
    public async Task Delete_OwnFile_Returns204()
    {
        const string userId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
        var mediaFile = await SeedMediaFileAsync(ownerId: userId);
        var token = JwtTokenHelper.GenerateToken(userId: userId);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/media/{mediaFile.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_OtherUsersFile_Returns403()
    {
        var mediaFile = await SeedMediaFileAsync(ownerId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var token = JwtTokenHelper.GenerateToken(userId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/media/{mediaFile.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_UnknownId_Returns404()
    {
        var unknownId = Guid.NewGuid();
        var token = JwtTokenHelper.GenerateToken();

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"/media/{unknownId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_NoAuth_Returns401()
    {
        var response = await _client.DeleteAsync($"/media/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
