using System.Net.Http.Json;
using TravelAgency.Media.Application.Features.Upload;

namespace TravelAgency.Media.IntegrationTests.MediaController;

public class UploadTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UploadTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static MultipartFormDataContent BuildMultipartContent(
        byte[] fileBytes,
        string fileName,
        string contentType)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task Upload_WithValidJpegFile_Returns201Created()
    {
        _factory.ImageProcessingService.IsImage("image/jpeg").Returns(true);
        _factory.ImageProcessingService
            .ResizeAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream("resized"u8.ToArray())));

        var token = JwtTokenHelper.GenerateToken();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/upload");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = BuildMultipartContent(
            "fake-jpeg-content"u8.ToArray(),
            "photo.jpg",
            "image/jpeg");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UploadMediaResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.Url.Should().NotBeNullOrEmpty();
        body.FileName.Should().Be("photo.jpg");
        body.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Upload_WithPdfFile_Returns201Created()
    {
        _factory.ImageProcessingService.IsImage("application/pdf").Returns(false);

        var token = JwtTokenHelper.GenerateToken();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/upload");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = BuildMultipartContent(
            "fake-pdf-content"u8.ToArray(),
            "document.pdf",
            "application/pdf");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<UploadMediaResponse>();
        body.Should().NotBeNull();
        body!.FileName.Should().Be("document.pdf");
        body.ContentType.Should().Be("application/pdf");
        body.Thumbnails.Should().BeEmpty();
    }

    [Fact]
    public async Task Upload_WithNoJwtToken_Returns401()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/upload");
        request.Content = BuildMultipartContent(
            "fake-content"u8.ToArray(),
            "photo.jpg",
            "image/jpeg");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Upload_WithInvalidMimeType_Returns400()
    {
        var token = JwtTokenHelper.GenerateToken();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/upload");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = BuildMultipartContent(
            "hello, world"u8.ToArray(),
            "readme.txt",
            "text/plain");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
