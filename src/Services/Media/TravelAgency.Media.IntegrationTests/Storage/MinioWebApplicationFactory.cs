using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Media.Infrastructure.Persistence;
using TravelAgency.Media.Infrastructure.Repositories;

namespace TravelAgency.Media.IntegrationTests.Storage;

/// <summary>
/// Media с настоящим IAmazonS3, S3StorageService и ImageProcessingService.
/// Подменяются только база (SQLite в памяти) и проверка JWT, как в <see cref="CustomWebApplicationFactory"/>.
/// Настройки передаются через UseSetting: AddMediaInfrastructure читает Storage сразу при регистрации.
/// </summary>
public sealed class MinioWebApplicationFactory(
    string serviceUrl,
    string accessKey,
    string secretKey,
    string bucketName) : WebApplicationFactory<Program>
{
    private const string SigningKey = "test-signing-key-must-be-at-least-32-chars-long!";

    private readonly SqliteConnection _connection = OpenConnection();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:MediaDb", "Host=localhost;Database=travel_media_test");
        builder.UseSetting("ASPNETCORE_RUN_MIGRATIONS", "false");
        builder.UseSetting("JwtSettings:Issuer", "test-issuer");
        builder.UseSetting("JwtSettings:Audience", "test-audience");
        builder.UseSetting("JwtSettings:SigningKey", SigningKey);
        builder.UseSetting("JwtSettings:ValidateLifetime", "false");
        builder.UseSetting("Storage:ServiceUrl", serviceUrl);
        builder.UseSetting("Storage:AccessKey", accessKey);
        builder.UseSetting("Storage:SecretKey", secretKey);
        builder.UseSetting("Storage:BucketName", bucketName);
        builder.UseSetting("Storage:ForcePathStyle", "true");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<MediaDbContext>>();
            services.RemoveAll<MediaDbContext>();
            var sqliteOptions = new DbContextOptionsBuilder<MediaDbContext>()
                .UseSqlite(_connection)
                .Options;
            services.AddScoped<MediaDbContext>(_ => new MediaDbContext(sqliteOptions));
            services.AddScoped<DbContextOptions<MediaDbContext>>(_ => sqliteOptions);
            services.AddScoped<IMediaFileRepository, MediaFileRepository>();

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });
        });
    }

    /// <summary>HttpClient после создания схемы SQLite.</summary>
    public new HttpClient CreateClient()
    {
        using (var scope = Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<MediaDbContext>().Database.EnsureCreated();
        }

        return base.CreateClient();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }
}
