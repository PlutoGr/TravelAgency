using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using TravelAgency.Catalog.API.Middleware;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Catalog.IntegrationTests.Helpers;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Catalog.IntegrationTests.Controllers;

/// <summary>
/// Добавление дочерних строк к уже сохранённому туру на настоящем Postgres.
/// Без ValueGeneratedNever EF шлёт UPDATE и SaveChanges падает.
/// </summary>
public sealed class TourChildRowsPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("catalog_manage_children")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplication? _app;
    private TestServer? _server;
    private readonly FakeMediaFilesClient _media = new();
    private readonly Guid _managerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var settings = new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
            ["ConnectionStrings:CatalogDb"] = _postgres.GetConnectionString(),
            ["GrpcSettings:InternalServiceToken"] = "test-internal-token",
            ["GrpcClients:MediaServiceUrl"] = "http://media-service:8081",
            ["Serilog:MinimumLevel:Default"] = "Warning"
        };

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Configuration.AddInMemoryCollection(settings);
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Services.AddSingleton<IExceptionMapper, CatalogExceptionMapper>();
        Program.ConfigureServices(builder.Services, builder.Configuration);
        builder.Services.RemoveAll<IMediaFilesClient>();
        builder.Services.AddSingleton<IMediaFilesClient>(_media);
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "TestIssuer",
                ValidAudience = "TestAudience",
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("TestSigningKeyWithAtLeast32CharactersForHMAC")),
                ClockSkew = TimeSpan.Zero,
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };
        });

        _app = builder.Build();
        Program.ConfigurePipeline(_app);
        await _app.StartAsync();
        _server = (TestServer)_app.Services.GetRequiredService<IServer>();

        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await db.Database.MigrateAsync();
        _media.Reset(_managerId.ToString());
    }

    public async Task DisposeAsync()
    {
        _server?.Dispose();
        if (_app is not null)
            await _app.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task PutPricesAndImages_OnExistingTour_InsertsRows()
    {
        var client = _server!.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            Token());

        var createdResponse = await client.PostAsJsonAsync(
            "/catalog/manage/tours",
            new CreateTourDraftRequest(null, null, null, null, null, null, null, null));
        createdResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createdResponse.Content.ReadFromJsonAsync<TourManageDto>();
        created.Should().NotBeNull();

        var from = DateTime.UtcNow.AddDays(15);
        var pricesResponse = await SendAsync(
            client,
            HttpMethod.Put,
            $"/catalog/manage/tours/{created!.Id}/prices",
            new UpdateTourManagePricesRequest(
            [
                new TourOfferInput(from, from.AddDays(7), 99000m, "RUB", 12)
            ]),
            created.Etag);
        pricesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var priced = await pricesResponse.Content.ReadFromJsonAsync<TourManageDto>();
        priced.Should().NotBeNull();
        priced!.Offers.Should().ContainSingle();

        var fileId = Guid.NewGuid();
        var imagesResponse = await SendAsync(
            client,
            HttpMethod.Put,
            $"/catalog/manage/tours/{created.Id}/images",
            new UpdateTourImagesRequest([new TourImageInput(fileId, 0, false, "Пляж")]),
            priced.Etag);
        imagesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var withImage = await imagesResponse.Content.ReadFromJsonAsync<TourManageDto>();
        withImage.Should().NotBeNull();
        withImage!.Images.Should().ContainSingle();

        await using var scope = _app!.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var offerId = priced.Offers[0].Id;
        var imageId = withImage.Images[0].Id;

        (await db.TourOffers.AsNoTracking().CountAsync(o => o.Id == offerId && o.TourId == created.Id))
            .Should().Be(1);
        (await db.TourImages.AsNoTracking().CountAsync(i => i.Id == imageId && i.TourId == created.Id && i.MediaFileId == fileId))
            .Should().Be(1);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        object body,
        string etag)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return await client.SendAsync(request);
    }

    private string Token()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TestSigningKeyWithAtLeast32CharactersForHMAC"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, _managerId.ToString()),
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Manager"),
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, _managerId.ToString())
        };
        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
