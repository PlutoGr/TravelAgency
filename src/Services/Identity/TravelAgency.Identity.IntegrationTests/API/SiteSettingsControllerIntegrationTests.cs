using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Infrastructure.Caching;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.IntegrationTests.API;

public class SiteSettingsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly Guid _adminId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public SiteSettingsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
        await ResetAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetPublic_WithoutAuth_ReturnsDefaultsAndCacheControl()
    {
        await ResetAsync();
        var response = await _client.GetAsync("/identity/site-settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.Public.Should().BeTrue();
        response.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(60));

        var body = await response.Content.ReadFromJsonAsync<SiteSettingsPublicDto>();
        body!.SiteName.Should().Be(SiteSettings.DefaultSiteName);
        body.SupportPhone.Should().BeNull();
        body.ContactEmail.Should().BeNull();
        body.SocialLinks.Should().BeEmpty();
        body.BannerEnabled.Should().BeFalse();
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("updatedByUserId");
        json.Should().NotContain("smtp");
    }

    [Fact]
    public async Task Update_AsAdmin_PersistsPhoneAuditAndPublicRead()
    {
        await ResetAsync();
        var response = await SendAsync(HttpMethod.Put, "/identity/site-settings/manage", AppRoles.Admin, ValidBody(phone: "+79991234567"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var saved = await response.Content.ReadFromJsonAsync<SiteSettingsAdminDto>();
        saved!.SupportPhone.Should().Be("+79991234567");
        saved.Message.Should().Be(SiteSettingsText.SavedNotice);
        saved.UpdatedByUserId.Should().Be(_adminId);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var row = await db.SiteSettings.AsNoTracking().SingleAsync();
        row.SupportPhone.Should().Be("+79991234567");
        var audit = await db.SiteSettingsAudits.AsNoTracking().SingleAsync();
        audit.FieldName.Should().Be(nameof(SiteSettings.SupportPhone));
        audit.OldValue.Should().BeNull();
        audit.NewValue.Should().Be("+79991234567");
        audit.ChangedByUserId.Should().Be(_adminId);

        var auditResponse = await SendAsync(HttpMethod.Get, "/identity/site-settings/manage/audit", AppRoles.Admin);
        auditResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var journal = await auditResponse.Content.ReadFromJsonAsync<List<SiteSettingsAuditItemDto>>();
        journal.Should().ContainSingle();
        journal![0].FieldName.Should().Be(nameof(SiteSettings.SupportPhone));

        var publicResponse = await _client.GetAsync("/identity/site-settings");
        var published = await publicResponse.Content.ReadFromJsonAsync<SiteSettingsPublicDto>();
        published!.SupportPhone.Should().Be("+79991234567");
    }

    [Fact]
    public async Task Update_WithPhoneNotE164_Returns400AndDoesNotChangeDatabase()
    {
        await ResetAsync();
        await SendAsync(HttpMethod.Put, "/identity/site-settings/manage", AppRoles.Admin, ValidBody(phone: "+79990001122"));

        var response = await SendAsync(HttpMethod.Put, "/identity/site-settings/manage", AppRoles.Admin, ValidBody(phone: "8999"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("supportPhone");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var row = await db.SiteSettings.AsNoTracking().SingleAsync();
        row.SupportPhone.Should().Be("+79990001122");
    }

    [Fact]
    public async Task Update_WithInvalidEmailAndHttpLink_Returns400()
    {
        await ResetAsync();
        var email = await SendAsync(
            HttpMethod.Put,
            "/identity/site-settings/manage",
            AppRoles.Admin,
            ValidBody(email: "not-an-email"));
        email.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await email.Content.ReadAsStringAsync()).Should().Contain("contactEmail");

        var link = await SendAsync(
            HttpMethod.Put,
            "/identity/site-settings/manage",
            AppRoles.Admin,
            ValidBody(links: new[] { new { label = "Сайт", url = "http://example.com" } }));
        link.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await link.Content.ReadAsStringAsync()).Should().Contain("socialLinks[0].url");
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Client)]
    public async Task Manage_AsNonAdmin_Returns403(string role)
    {
        var read = await SendAsync(HttpMethod.Get, "/identity/site-settings/manage", role);
        var write = await SendAsync(HttpMethod.Put, "/identity/site-settings/manage", role, ValidBody());

        read.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        write.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manage_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/identity/site-settings/manage");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_IgnoresSecretFields()
    {
        await ResetAsync();
        var body = new
        {
            siteName = "TravelAgency",
            supportPhone = "+79991112233",
            contactEmail = (string?)null,
            officeAddress = (string?)null,
            workingHours = (string?)null,
            socialLinks = Array.Empty<object>(),
            bannerText = (string?)null,
            bannerEnabled = false,
            smtpPassword = "super-secret-value",
            apiKey = "secret-api-key"
        };

        var response = await SendAsync(HttpMethod.Put, "/identity/site-settings/manage", AppRoles.Admin, body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("super-secret-value");
        json.Should().NotContain("secret-api-key");
        json.Should().NotContain("smtpPassword");

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var row = await db.SiteSettings.AsNoTracking().SingleAsync();
        var stored = JsonSerializer.Serialize(row);
        stored.Should().NotContain("super-secret-value");
        stored.Should().NotContain("secret-api-key");
    }

    [Fact]
    public async Task Audit_ReturnsAtMost50NewestFirst()
    {
        await ResetAsync();
        var userId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var now = DateTimeOffset.UtcNow;
            for (var index = 0; index < 55; index++)
            {
                db.SiteSettingsAudits.Add(new SiteSettingsAudit
                {
                    FieldName = nameof(SiteSettings.SiteName),
                    OldValue = index.ToString(),
                    NewValue = (index + 1).ToString(),
                    ChangedByUserId = userId,
                    ChangedAt = now.AddMinutes(index)
                });
            }

            await db.SaveChangesAsync();
        }

        var response = await SendAsync(HttpMethod.Get, "/identity/site-settings/manage/audit", AppRoles.Admin);
        var journal = await response.Content.ReadFromJsonAsync<List<SiteSettingsAuditItemDto>>();

        journal.Should().HaveCount(50);
        journal![0].NewValue.Should().Be("55");
        journal[^1].NewValue.Should().Be("6");
    }

    [Fact]
    public async Task Update_InvalidatesPublicCache()
    {
        await ResetAsync();
        var first = await SendAsync(HttpMethod.Put, "/identity/site-settings/manage", AppRoles.Admin, ValidBody(phone: "+79990000001"));
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var cached = await _client.GetAsync("/identity/site-settings");
        (await cached.Content.ReadFromJsonAsync<SiteSettingsPublicDto>())!.SupportPhone.Should().Be("+79990000001");

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var row = await db.SiteSettings.SingleAsync();
            row.SupportPhone = "+79990000002";
            await db.SaveChangesAsync();
        }

        var stillCached = await _client.GetAsync("/identity/site-settings");
        (await stillCached.Content.ReadFromJsonAsync<SiteSettingsPublicDto>())!.SupportPhone.Should().Be("+79990000001");

        var second = await SendAsync(HttpMethod.Put, "/identity/site-settings/manage", AppRoles.Admin, ValidBody(phone: "+79990000003"));
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var fresh = await _client.GetAsync("/identity/site-settings");
        (await fresh.Content.ReadFromJsonAsync<SiteSettingsPublicDto>())!.SupportPhone.Should().Be("+79990000003");
    }

    [Fact]
    public async Task DistributedCache_RoundTripsPublicDtoWithTtlKey()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var sut = new DistributedSiteSettingsCache(cache, NullLogger<DistributedSiteSettingsCache>.Instance);
        var dto = new SiteSettingsPublicDto(
            "Агентство",
            "+79991234567",
            "user@example.com",
            null,
            null,
            [new SocialLinkDto("Сайт", "https://example.com")],
            "<script>alert(1)</script>",
            true);

        await sut.SetAsync(dto);
        var stored = await cache.GetAsync(SiteSettingsCache.PublicKey);
        stored.Should().NotBeNull();

        var loaded = await sut.TryGetAsync();
        loaded.Should().BeEquivalentTo(dto);

        await sut.RemoveAsync();
        (await sut.TryGetAsync()).Should().BeNull();
    }

    private async Task ResetAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.SiteSettingsAudits.RemoveRange(await db.SiteSettingsAudits.ToListAsync());
        await db.SaveChangesAsync();

        var row = await db.SiteSettings.SingleOrDefaultAsync();
        if (row is null)
        {
            db.SiteSettings.Add(SiteSettings.CreateDefault(SiteSettings.SeedUpdatedAt));
        }
        else
        {
            row.SiteName = SiteSettings.DefaultSiteName;
            row.SupportPhone = null;
            row.ContactEmail = null;
            row.OfficeAddress = null;
            row.WorkingHours = null;
            row.SocialLinksJson = "[]";
            row.BannerText = null;
            row.BannerEnabled = false;
            row.UpdatedAt = SiteSettings.SeedUpdatedAt;
            row.UpdatedByUserId = null;
        }

        await db.SaveChangesAsync();

        var cache = scope.ServiceProvider.GetRequiredService<ISiteSettingsPublicCache>();
        await cache.RemoveAsync();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string role, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(role));
        if (body is not null)
            request.Content = JsonContent.Create(body);

        return await _client.SendAsync(request);
    }

    private string Token(string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _adminId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static object ValidBody(
        string? phone = null,
        string? email = null,
        object[]? links = null) => new
    {
        siteName = "TravelAgency",
        supportPhone = phone,
        contactEmail = email,
        officeAddress = (string?)null,
        workingHours = (string?)null,
        socialLinks = links ?? [],
        bannerText = (string?)null,
        bannerEnabled = false
    };
}
