using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.IntegrationTests.MediaController;

public class GetByIdTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GetByIdTests(CustomWebApplicationFactory factory)
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
    public async Task GetById_ExistingMedia_Returns200WithContent()
    {
        var mediaFile = await SeedMediaFileAsync();

        var response = await _client.GetAsync($"/media/{mediaFile.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("image/jpeg");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetById_UnknownId_Returns404()
    {
        var unknownId = Guid.NewGuid();

        var response = await _client.GetAsync($"/media/{unknownId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NoAuthRequired_Returns200()
    {
        var mediaFile = await SeedMediaFileAsync();

        // Explicitly create a client with no default auth headers
        var anonymousClient = _factory.CreateClient();
        var response = await anonymousClient.GetAsync($"/media/{mediaFile.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
