using System.Net.Http.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TravelAgency.Contracts.Grpc.Media;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Media.Infrastructure.GrpcServices;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.IntegrationTests.Storage;

/// <summary>
/// #59: POST /media/upload на настоящий MinIO по http.
/// На старом коде (DisablePayloadSigning = true) SDK бросал AmazonClientException
/// «When DisablePayloadSigning is true, the request must be sent over HTTPS» и ответ был 500.
/// </summary>
public class MinioUploadTests(MinioStorageFixture fixture) : IClassFixture<MinioStorageFixture>
{
    private const string UserId = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

    [Theory]
    [InlineData(null, AppRoles.Client)]
    [InlineData("tour-image", AppRoles.Manager)]
    public async Task Upload_Png_StoresExactBytesInMinio(string? purpose, string role)
    {
        // Шум почти не сжимается: PNG выходит в сотни КБ, это несколько aws-chunk по 80 КБ.
        var original = CreateNoisePng(400, 300);
        original.Length.Should().BeGreaterThan(256 * 1024);

        var client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            purpose is null ? "/media/upload" : $"/media/upload?purpose={purpose}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", JwtTokenHelper.GenerateToken(UserId, role));
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(original);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "noise.png");
        request.Content = content;

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<UploadMediaResponse>();
        body.Should().NotBeNull();

        string storageKey;
        string[] thumbnailKeys;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var media = await scope.ServiceProvider.GetRequiredService<IMediaFileRepository>().GetByIdAsync(body!.Id);
            media.Should().NotBeNull();
            storageKey = media!.StorageKey;
            thumbnailKeys = media.Thumbnails.Select(t => t.StorageKey).ToArray();
        }

        using var s3 = fixture.CreateS3Client();

        // Читаем из MinIO мимо Media и сверяем байты: неверная подпись или разбивка на chunk
        // могла бы сохранить испорченный файл при ответе 201.
        using (var stored = await s3.GetObjectAsync(MinioStorageFixture.BucketName, storageKey))
        {
            stored.ContentLength.Should().Be(original.Length);
            using var ms = new MemoryStream();
            await stored.ResponseStream.CopyToAsync(ms);
            var actual = ms.ToArray();
            actual.Length.Should().Be(original.Length);
            actual.AsSpan().SequenceEqual(original).Should().BeTrue("MinIO должен хранить файл байт в байт");
        }

        thumbnailKeys.Should().NotBeEmpty();
        foreach (var key in thumbnailKeys)
        {
            var meta = await s3.GetObjectMetadataAsync(MinioStorageFixture.BucketName, key);
            meta.ContentLength.Should().BeGreaterThan(0, $"превью {key} должно лежать в MinIO");
        }
    }

    [Fact]
    public async Task Upload_PngOfKnownSize_ReturnsOriginalAndThumbnailDimensions_AndGrpcMatches()
    {
        const int width = 1000;
        const int height = 500;
        var original = CreateSolidPng(width, height);

        var client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/upload");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTokenHelper.GenerateToken(UserId, AppRoles.Client));
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(original);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "known.png");
        request.Content = content;

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<UploadMediaResponse>();
        body.Should().NotBeNull();
        body!.Width.Should().Be(width);
        body.Height.Should().Be(height);
        body.Thumbnails.Select(thumb => (thumb.Width, thumb.Height)).Should().Equal((200, 100), (800, 400));

        string storageKey;
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var media = await scope.ServiceProvider.GetRequiredService<IMediaFileRepository>().GetByIdAsync(body.Id);
            media.Should().NotBeNull();
            media!.Width.Should().Be(body.Width);
            media.Height.Should().Be(body.Height);
            media.Thumbnails.Should().HaveCount(2);
            media.Thumbnails.Select(thumb => (thumb.Width, thumb.Height)).Should().Equal((200, 100), (800, 400));
            media.Thumbnails.Select(thumb => thumb.StorageKey).Should().OnlyHaveUniqueItems();
            storageKey = media.StorageKey;

            var grpc = new MediaGrpcService(scope.ServiceProvider.GetRequiredService<IMediaFileRepository>());
            var listed = await grpc.GetMediaFiles(
                new GetMediaFilesRequest { Ids = { body.Id.ToString() } },
                new StubServerCallContext());
            listed.Files.Should().ContainSingle();
            listed.Files[0].Width.Should().Be(body.Width);
            listed.Files[0].Height.Should().Be(body.Height);
        }

        using var s3 = fixture.CreateS3Client();
        var stored = await s3.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = MinioStorageFixture.BucketName,
            Prefix = storageKey
        });
        stored.S3Objects.Select(item => item.Key).Should().HaveCount(3);
    }

    [Fact]
    public async Task TourImage_SlotNotWiderThanOriginal_ReturnsStoredPixelsWithout404()
    {
        var small = await UploadTourImageAsync(200, 200);
        await AssertPublicPixelsAsync(small, "w800", 200, 200);
        await AssertPublicPixelsAsync(small, "w1600", 200, 200);

        var mid = await UploadTourImageAsync(600, 400);
        await AssertPublicPixelsAsync(mid, "w800", 600, 400);
        await AssertPublicPixelsAsync(mid, "w1600", 600, 400);

        var wide = await UploadTourImageAsync(1600, 1000);
        await AssertPublicPixelsAsync(wide, "w800", 800, 500);
    }

    private static byte[] CreateNoisePng(int width, int height)
    {
        var random = new Random(59);
        using var image = new Image<Rgb24>(width, height);
        image.ProcessPixelRows(rows =>
        {
            for (var y = 0; y < rows.Height; y++)
            {
                var row = rows.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                    row[x] = new Rgb24((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
            }
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private async Task<Guid> UploadTourImageAsync(int width, int height)
    {
        var original = CreateSolidPng(width, height);
        var client = fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/media/upload?purpose=tour-image");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            JwtTokenHelper.GenerateToken(UserId, AppRoles.Manager));
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(original);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "tour.png");
        request.Content = content;

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<UploadMediaResponse>();
        body.Should().NotBeNull();
        body!.Width.Should().Be(width);
        body.Height.Should().Be(height);

        using var scope = fixture.Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
        var media = await repository.GetByIdAsync(body.Id);
        media.Should().NotBeNull();
        media!.Thumbnails.Select(thumb => thumb.SizeCode).Should().BeEquivalentTo("w200", "w800", "w1600");
        media.MarkPublic();
        await repository.SaveChangesAsync();
        return body.Id;
    }

    private async Task AssertPublicPixelsAsync(Guid id, string size, int width, int height)
    {
        var client = fixture.Factory.CreateClient();
        var response = await client.GetAsync($"/media/files/{id}/{size}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var bytes = await response.Content.ReadAsByteArrayAsync();
        var info = Image.Identify(new MemoryStream(bytes));
        info.Should().NotBeNull();
        info!.Width.Should().Be(width);
        info.Height.Should().Be(height);
    }

    private static byte[] CreateSolidPng(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }

    private sealed class StubServerCallContext : ServerCallContext
    {
        protected override string MethodCore => "GetMediaFiles";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "127.0.0.1";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore { get; } = [];
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore { get; } = [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => throw new NotSupportedException();
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
            throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
