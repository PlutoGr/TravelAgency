using System.Net.Http.Json;
using Amazon.S3;
using Microsoft.Extensions.DependencyInjection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Media.Domain.Interfaces;
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
}
