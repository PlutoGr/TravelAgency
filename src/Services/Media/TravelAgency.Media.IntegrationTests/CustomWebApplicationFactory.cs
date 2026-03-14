using System.Text;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Media.Infrastructure.Repositories;

namespace TravelAgency.Media.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public IStorageService StorageService { get; } = Substitute.For<IStorageService>();
    public IImageProcessingService ImageProcessingService { get; } = Substitute.For<IImageProcessingService>();

    public CustomWebApplicationFactory()
    {
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
            // Replace IAmazonS3 so the S3StorageService never connects to real AWS
            services.RemoveAll<IAmazonS3>();
            services.AddSingleton(Substitute.For<IAmazonS3>());

            // Replace real S3 with mock
            services.RemoveAll<IStorageService>();
            services.AddScoped(_ => StorageService);

            // Replace real image processor with mock
            services.RemoveAll<IImageProcessingService>();
            services.AddScoped(_ => ImageProcessingService);

            // Fresh in-memory repository (singleton so all requests share state within a test)
            services.RemoveAll<IMediaFileRepository>();
            services.AddSingleton<IMediaFileRepository, InMemoryMediaFileRepository>();

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
    /// Helper to access the singleton in-memory repository and seed test data.
    /// </summary>
    public IMediaFileRepository GetRepository()
    {
        using var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IMediaFileRepository>();
    }
}
