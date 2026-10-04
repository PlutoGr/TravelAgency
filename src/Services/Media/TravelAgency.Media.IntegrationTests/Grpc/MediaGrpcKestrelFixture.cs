using System.Net;
using System.Net.Sockets;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Testcontainers.PostgreSql;
using TravelAgency.Media.API.Extensions;
using TravelAgency.Media.API.Hosting;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Media.Infrastructure.Persistence;

namespace TravelAgency.Media.IntegrationTests.Grpc;

/// <summary>
/// Boots Media on real Kestrel: HTTP/1.1 on a public port and HTTP/2 on <see cref="MediaPorts.Grpc"/>.
/// WebApplicationFactory's TestServer does not exercise cleartext HTTP/2.
/// </summary>
public sealed class MediaGrpcKestrelFixture : IAsyncLifetime
{
    public const string ServiceToken = "test-internal-token";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("travel_media_grpc")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplication? _app;

    public string Address { get; private set; } = string.Empty;

    public string GrpcAddress => $"http://127.0.0.1:{MediaPorts.Grpc}";

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var port = GetFreePort();
        Address = $"http://127.0.0.1:{port}";

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(Program).Assembly.GetName().Name
        });

        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseUrls(Address);
        Program.ConfigureHost(builder.WebHost);
        builder.Host.AddMediaSerilog();

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:MediaDb"] = _postgres.GetConnectionString(),
            ["JwtSettings:Issuer"] = "test-issuer",
            ["JwtSettings:Audience"] = "test-audience",
            ["JwtSettings:SigningKey"] = "test-signing-key-must-be-at-least-32-chars-long!",
            ["JwtSettings:ValidateLifetime"] = "false",
            ["GrpcSettings:InternalServiceToken"] = ServiceToken,
            ["Storage:ServiceUrl"] = "http://127.0.0.1:9000",
            ["Storage:AccessKey"] = "test",
            ["Storage:SecretKey"] = "test",
            ["Storage:BucketName"] = "test-media",
            ["Storage:ForcePathStyle"] = "true"
        });

        Program.ConfigureServices(builder.Services, builder.Configuration);

        var s3 = Substitute.For<IAmazonS3>();
        s3.ListBucketsAsync(Arg.Any<CancellationToken>())
            .Returns(new ListBucketsResponse
            {
                Buckets = [new S3Bucket { BucketName = "test-media" }]
            });
        builder.Services.RemoveAll<IAmazonS3>();
        builder.Services.AddSingleton(s3);

        _app = builder.Build();
        Program.ConfigurePipeline(_app);

        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            await db.Database.MigrateAsync();
        }

        await _app.StartAsync();
    }

    public async Task<MediaFile> SeedTourImageAsync(string? ownerId = null)
    {
        await using var scope = _app!.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
        var file = MediaFile.Create(
            "cover.jpg",
            "image/jpeg",
            2048,
            "owner/cover.jpg",
            ownerId ?? Guid.NewGuid().ToString(),
            MediaPurposes.TourImage,
            width: 2400,
            height: 1350);
        file.AddThumbnail("owner/cover.jpg-preview-w200", 200, 113, TourImageSizes.W200);
        file.AddThumbnail("owner/cover.jpg-preview-w800", 800, 450, TourImageSizes.W800);
        file.AddThumbnail("owner/cover.jpg-preview-w1600", 1600, 900, TourImageSizes.W1600);
        await repo.AddAsync(file);
        await repo.SaveChangesAsync();
        return file;
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port == MediaPorts.Grpc ? GetFreePort() : port;
    }
}
