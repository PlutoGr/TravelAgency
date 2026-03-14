using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Media.Application.Features.Presign;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.IntegrationTests.MediaController;

public class PresignTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PresignTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<MediaFile> SeedMediaFileAsync(string ownerId = "test-user-id")
    {
        var file = MediaFile.Create("photo.jpg", "image/jpeg", 1024, "storage/key/photo.jpg", ownerId);

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
        await repo.AddAsync(file);

        return file;
    }

    [Fact]
    public async Task Presign_ExistingMedia_Returns200WithUrl()
    {
        var mediaFile = await SeedMediaFileAsync();
        var token = JwtTokenHelper.GenerateToken();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/presign");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new PresignMediaQuery(mediaFile.Id));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PresignMediaResponse>();
        body.Should().NotBeNull();
        body!.Url.Should().NotBeNullOrEmpty();
        body.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Presign_UnknownId_Returns404()
    {
        var token = JwtTokenHelper.GenerateToken();
        var unknownId = Guid.NewGuid();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/presign");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(new PresignMediaQuery(unknownId));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Presign_NoAuth_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/presign");
        request.Content = JsonContent.Create(new PresignMediaQuery(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
