using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Media.Infrastructure.Persistence;
using TravelAgency.Media.Infrastructure.Repositories;

namespace TravelAgency.Media.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public IStorageService StorageService { get; } = Substitute.For<IStorageService>();
    public IImageProcessingService ImageProcessingService { get; } = Substitute.For<IImageProcessingService>();

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Set the environment variable so AddMediaAuthentication doesn't throw at startup.
        // This is read eagerly during service registration, before ConfigureAppConfiguration runs.
        Environment.SetEnvironmentVariable("JwtSettings__SigningKey", "test-signing-key-must-be-at-least-32-chars-long!");

        StorageService
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("storage-key"));

        StorageService
            .GeneratePresignedUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult("https://example.com/file"));

        StorageService
            .DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var ms = new MemoryStream("fake-file-content"u8.ToArray());
                return Task.FromResult<Stream>(ms);
            });

        StorageService
            .DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        ImageProcessingService.IsImage(Arg.Any<string>()).Returns(false);

        ImageProcessingService
            .ResizeAsync(Arg.Any<Stream>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Stream>(new MemoryStream("resized"u8.ToArray())));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "test-issuer",
                ["JwtSettings:Audience"] = "test-audience",
                ["JwtSettings:SigningKey"] = "test-signing-key-must-be-at-least-32-chars-long!",
                ["JwtSettings:ValidateLifetime"] = "false",
                ["Storage:ServiceUrl"] = "http://localhost:9000",
                ["Storage:AccessKey"] = "test",
                ["Storage:SecretKey"] = "test",
                ["Storage:BucketName"] = "test",
                ["Storage:PresignTtlMinutes"] = "60",
                ["Upload:MaxFileSizeBytes"] = "10485760",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace IAmazonS3 so the S3StorageService never connects to real AWS.
            // Configure mock so BucketInitializer sees bucket as existing and skips PutBucketAsync.
            var s3Mock = Substitute.For<IAmazonS3>();
            s3Mock.ListBucketsAsync(Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new ListBucketsResponse
                {
                    Buckets = [new S3Bucket { BucketName = "test" }]
                }));
            services.RemoveAll<IAmazonS3>();
            services.AddSingleton(s3Mock);

            // Replace real S3 with mock
            services.RemoveAll<IStorageService>();
            services.AddScoped(_ => StorageService);

            // Replace real image processor with mock
            services.RemoveAll<IImageProcessingService>();
            services.AddScoped(_ => ImageProcessingService);

            // Replace DbContext with SQLite in-memory for tests.
            // Passing the open SqliteConnection keeps the in-memory database alive across requests.
            services.RemoveAll<DbContextOptions<MediaDbContext>>();
            services.RemoveAll<MediaDbContext>();
            var sqliteOptions = new DbContextOptionsBuilder<MediaDbContext>()
                .UseSqlite(_connection)
                .Options;
            services.AddScoped<MediaDbContext>(_ => new MediaDbContext(sqliteOptions));
            services.AddScoped<DbContextOptions<MediaDbContext>>(_ => sqliteOptions);
            services.AddScoped<IMediaFileRepository, MediaFileRepository>();

            // Override JWT validation so test-issued tokens are accepted
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = false,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "test-issuer",
                    ValidAudience = "test-audience",
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("test-signing-key-must-be-at-least-32-chars-long!")),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });
        });
    }

    /// <summary>
    /// Creates an HttpClient with the database schema ensured. Use this instead of base.CreateClient()
    /// so SQLite in-memory is initialized before the first request.
    /// </summary>
    public new HttpClient CreateClient()
    {
        EnsureDatabaseCreated();
        return base.CreateClient();
    }

    /// <summary>
    /// Helper to access the repository and seed test data.
    /// </summary>
    public IMediaFileRepository GetRepository()
    {
        EnsureDatabaseCreated();
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
    }

    private bool _dbCreated;

    /// <summary>
    /// Ensures the SQLite in-memory database schema exists. Call before seeding or when using the repository.
    /// </summary>
    public void EnsureDatabaseCreated()
    {
        if (_dbCreated) return;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        db.Database.EnsureCreated();
        _dbCreated = true;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
