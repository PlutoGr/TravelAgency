using System.Net.Http.Json;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.IntegrationTests.MediaController;

public class TourImageTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string OwnerId = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public TourImageTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.ImageProcessingService
            .GetDimensionsAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(new ImageDimensions(2000, 1000));
        _factory.ImageProcessingService
            .ResizeWithinAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var maxWidth = ci.ArgAt<int>(1);
                var width = Math.Min(2000, maxWidth);
                return new ResizedImage(new MemoryStream("preview"u8.ToArray()), width, width / 2, "image/jpeg");
            });
    }

    [Fact]
    public async Task TourImage_DraftIs404_PublishedIsCached_UnpublishDoesNotHideIt()
    {
        var uploaded = await UploadTourImageAsync(AppRoles.Manager, OwnerId);

        var draft = await _client.GetAsync($"/media/files/{uploaded.Id}/w200");
        draft.StatusCode.Should().Be(HttpStatusCode.NotFound);
        CacheControl(draft).Should().Contain("no-store");

        await MarkPublicAsync(uploaded.Id);

        var published = await _client.GetAsync($"/media/files/{uploaded.Id}/w800");
        published.StatusCode.Should().Be(HttpStatusCode.OK);
        published.Content.Headers.ContentType?.MediaType.Should().Be("image/jpeg");
        CacheControl(published).Should().Be("public, max-age=31536000, immutable");
        (await published.Content.ReadAsStringAsync()).Should().Be("fake-file-content");

        var manage = await SendAsync(HttpMethod.Get, $"/media/manage/files/{uploaded.Id}/w1600", AppRoles.Manager, OwnerId);
        manage.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertPrivateNoStore(manage);
    }

    [Fact]
    public async Task TourImage_Upload_RequiresManagerOrAdmin()
    {
        var anonymous = await UploadRawAsync(null, null, "image/jpeg", [0xFF, 0xD8, 0xFF, 0x00]);
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var client = await UploadRawAsync(AppRoles.Client, OwnerId, "image/jpeg", [0xFF, 0xD8, 0xFF, 0x00]);
        client.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TourImage_Gif_IsRejected()
    {
        var response = await UploadRawAsync(
            AppRoles.Manager, OwnerId, "image/gif", [0x47, 0x49, 0x46, 0x38, 0x39, 0x61]);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task OtherPurpose_PublicUrl_Is404_EvenAfterMarkPublic()
    {
        var token = JwtTokenHelper.GenerateToken(OwnerId, AppRoles.Client);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/upload");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = FileContent([0x25, 0x50, 0x44, 0x46, 0x2D], "doc.pdf", "application/pdf");

        var uploaded = await _client.SendAsync(request);
        uploaded.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await uploaded.Content.ReadFromJsonAsync<UploadMediaResponse>();

        await MarkPublicAsync(body!.Id);

        var response = await _client.GetAsync($"/media/files/{body.Id}/w200");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Manage_OtherManagerAndClientForbidden_GuestUnauthorized_AdminAllowed()
    {
        var uploaded = await UploadTourImageAsync(AppRoles.Admin, OwnerId);

        var otherManager = await SendAsync(
            HttpMethod.Get, $"/media/manage/files/{uploaded.Id}/w200", AppRoles.Manager, Guid.NewGuid().ToString());
        otherManager.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var client = await SendAsync(
            HttpMethod.Get, $"/media/manage/files/{uploaded.Id}/w200", AppRoles.Client, Guid.NewGuid().ToString());
        client.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var guest = await _client.GetAsync($"/media/manage/files/{uploaded.Id}/w200");
        guest.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var admin = await SendAsync(
            HttpMethod.Get, $"/media/manage/files/{uploaded.Id}/w800", AppRoles.Admin, Guid.NewGuid().ToString());
        admin.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertPrivateNoStore(admin);
    }

    private async Task<UploadMediaResponse> UploadTourImageAsync(string role, string userId)
    {
        var response = await UploadRawAsync(role, userId, "image/jpeg", [0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00]);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("minio");
        json.Should().NotContain("X-Amz-");
        json.Should().NotContain("unsplash");
        json.Should().NotContain("http://");
        json.Should().NotContain("https://");
        var body = System.Text.Json.JsonSerializer.Deserialize<UploadMediaResponse>(
            json,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        body.Should().NotBeNull();
        body!.IsPublic.Should().BeFalse();
        body.Width.Should().Be(2000);
        return body;
    }

    private async Task<HttpResponseMessage> UploadRawAsync(string? role, string? userId, string contentType, byte[] bytes)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/upload?purpose=tour-image");
        if (role is not null && userId is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(userId, role));
        request.Content = FileContent(bytes, "photo.bin", contentType);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string role, string userId)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(userId, role));
        return await _client.SendAsync(request);
    }

    private async Task MarkPublicAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
        var file = await repo.GetByIdAsync(id);
        file.Should().NotBeNull();
        file!.MarkPublic();
        await repo.SaveChangesAsync();
    }

    private static MultipartFormDataContent FileContent(byte[] bytes, string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", fileName);
        return content;
    }

    private static void AssertPrivateNoStore(HttpResponseMessage response)
    {
        var cache = CacheControl(response);
        cache.Should().Contain("private");
        cache.Should().Contain("no-store");
        cache.Should().NotContain("public");
    }

    private static string CacheControl(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Cache-Control", out var values))
            return string.Join(", ", values);
        return string.Empty;
    }
}
