using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Infrastructure.Storage;

namespace TravelAgency.Media.UnitTests.Infrastructure;

/// <summary>#59: тело можно не подписывать только поверх https.</summary>
public class S3PayloadSigningTests
{
    [Theory]
    [InlineData("https://s3.example.com", true)]
    [InlineData("HTTPS://s3.example.com:9000/", true)]
    [InlineData("http://minio:9000", false)]
    [InlineData("http://127.0.0.1:9000/", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("minio:9000", false)]
    [InlineData("not a url", false)]
    public void CanDisablePayloadSigning_DependsOnScheme(string? serviceUrl, bool expected)
    {
        S3PayloadSigning.CanDisablePayloadSigning(serviceUrl).Should().Be(expected);
    }

    [Theory]
    [InlineData("http://minio:9000", false)]
    [InlineData("https://s3.example.com", true)]
    public async Task UploadAsync_SetsDisablePayloadSigning_ByServiceUrlScheme(string serviceUrl, bool expected)
    {
        var s3 = Substitute.For<IAmazonS3>();
        PutObjectRequest? sent = null;
        s3.PutObjectAsync(Arg.Do<PutObjectRequest>(r => sent = r), Arg.Any<CancellationToken>())
            .Returns(new PutObjectResponse());
        var service = new S3StorageService(s3, Options.Create(new StorageSettings
        {
            ServiceUrl = serviceUrl,
            BucketName = "media"
        }));

        await service.UploadAsync(new MemoryStream([1, 2, 3]), "key", "image/png");

        sent.Should().NotBeNull();
        sent!.DisablePayloadSigning.Should().Be(expected);
    }
}
