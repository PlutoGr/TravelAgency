using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Media.Application.Features.Presign;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.IntegrationTests.MediaController;

public class PresignTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string OwnerId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
    private const string OtherClientId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
    private const string StaffId = "cccccccc-cccc-cccc-cccc-cccccccccccc";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PresignTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<MediaFile> SeedMediaFileAsync(
        string ownerId = OwnerId,
        bool isPublic = false,
        string purpose = MediaPurposes.General)
    {
        _factory.EnsureDatabaseCreated();
        var file = MediaFile.Create(
            "photo.jpg", "image/jpeg", 1024, "storage/key/photo.jpg", ownerId, purpose);
        if (isPublic)
            file.MarkPublic();

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
        await repo.AddAsync(file);
        await repo.SaveChangesAsync();

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
    public async Task Presign_PublicGeneral_OtherClient_Returns404()
    {
        var mediaFile = await SeedMediaFileAsync(isPublic: true, purpose: MediaPurposes.General);

        var response = await SendAsync(mediaFile.Id, AppRoles.Client, OtherClientId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Presign_PublicTourImage_OtherClient_Returns200()
    {
        var mediaFile = await SeedMediaFileAsync(isPublic: true, purpose: MediaPurposes.TourImage);

        var response = await SendAsync(mediaFile.Id, AppRoles.Client, OtherClientId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PresignMediaResponse>();
        body!.Url.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Presign_Manager_OtherPrivateFile_Returns200()
    {
        var mediaFile = await SeedMediaFileAsync();

        var response = await SendAsync(mediaFile.Id, AppRoles.Manager, StaffId);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PresignMediaResponse>();
        body!.Url.Should().NotBeNullOrEmpty();
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

    private async Task<HttpResponseMessage> SendAsync(Guid id, string role, string userId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/presign");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTokenHelper.GenerateToken(userId, role));
        request.Content = JsonContent.Create(new PresignMediaQuery(id));
        return await _client.SendAsync(request);
    }
}
