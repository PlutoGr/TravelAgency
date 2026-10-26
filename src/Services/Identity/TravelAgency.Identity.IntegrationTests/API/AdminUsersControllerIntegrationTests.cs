using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Admin;
using TravelAgency.Identity.Application.Features.Auth;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Services;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.IntegrationTests.API;

public class AdminUsersControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";
    private const string Password = "Password1";
    private const string Phone = "+79991234567";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminUsersControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
        await ResetAsync(db);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/identity/admin/users");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.Client)]
    public async Task EveryEndpoint_AsNonAdmin_Returns403(string role)
    {
        var actor = await SeedAsync("staff-" + role + "@example.com", role == AppRoles.Manager ? UserRole.Manager : UserRole.Client);
        var target = await SeedAsync("target-" + role + "@example.com", UserRole.Client);

        var list = await SendAsync(HttpMethod.Get, "/identity/admin/users", actor.Id, role);
        var card = await SendAsync(HttpMethod.Get, $"/identity/admin/users/{target.Id}", actor.Id, role);
        var change = await SendAsync(HttpMethod.Put, $"/identity/admin/users/{target.Id}/role", actor.Id, role, new { role = "Manager" });
        var block = await SendAsync(HttpMethod.Post, $"/identity/admin/users/{target.Id}/block", actor.Id, role, new { reason = "спам" });
        var unblock = await SendAsync(HttpMethod.Post, $"/identity/admin/users/{target.Id}/unblock", actor.Id, role);

        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        card.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        change.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        block.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        unblock.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_SearchPagingAndDto_HidesSecrets()
    {
        var admin = await SeedAsync("admin-list@example.com", UserRole.Admin, firstName: "Админ", lastName: "Списка");
        await SeedAsync("unique-person@example.com", UserRole.Client, firstName: "Уникальный", lastName: "Клиент", phone: "+79990000001", passwordHash: "super-secret-hash");
        await SeedAsync("other-person@example.com", UserRole.Manager, firstName: "Другой", lastName: "Менеджер");
        for (var i = 0; i < 20; i++)
            await SeedAsync($"page-{i}@example.com", UserRole.Client, firstName: "Страница", lastName: i.ToString());

        var found = await SendAsync(HttpMethod.Get, "/identity/admin/users?q=Уникальный", admin.Id, AppRoles.Admin);
        found.StatusCode.Should().Be(HttpStatusCode.OK);
        var foundBody = await found.Content.ReadFromJsonAsync<AdminUserListDto>();
        foundBody!.Items.Should().ContainSingle();
        foundBody.Items[0].Email.Should().Be("unique-person@example.com");
        foundBody.PageSize.Should().Be(20);
        var raw = await found.Content.ReadAsStringAsync();
        raw.Should().NotContain("super-secret-hash");
        raw.Should().NotContain("passwordHash");
        raw.Should().NotContain("sessionVersion");

        var wildcard = await SendAsync(HttpMethod.Get, "/identity/admin/users?q=" + Uri.EscapeDataString("%"), admin.Id, AppRoles.Admin);
        var wildcardBody = await wildcard.Content.ReadFromJsonAsync<AdminUserListDto>();
        wildcardBody!.Total.Should().Be(0);

        var page = await SendAsync(HttpMethod.Get, "/identity/admin/users?role=Client&page=2", admin.Id, AppRoles.Admin);
        var pageBody = await page.Content.ReadFromJsonAsync<AdminUserListDto>();
        pageBody!.Page.Should().Be(2);
        pageBody.Items.Should().NotBeEmpty();
        pageBody.Total.Should().BeGreaterThan(20);
    }

    [Fact]
    public async Task ChangeRole_Downgrade_RevokesRefreshAndOldAccess()
    {
        var admin = await SeedAsync("admin-role@example.com", UserRole.Admin);
        var manager = await SeedAsync("manager-role@example.com", UserRole.Manager);
        var login = await LoginAsync(manager.Email);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokensDto>();

        var changed = await SendAsync(
            HttpMethod.Put,
            $"/identity/admin/users/{manager.Id}/role",
            admin.Id,
            AppRoles.Admin,
            new { role = "Client" });
        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await changed.Content.ReadFromJsonAsync<AdminUserMutationResponse>())!.Message.Should().Be(UserAdminText.RoleChanged);

        var me = await SendBearerAsync(HttpMethod.Get, "/identity/me", tokens!.AccessToken);
        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var refresh = await _client.PostAsJsonAsync("/identity/refresh", new { refreshToken = tokens.RefreshToken });
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var audit = await db.UserAdminAudits.AsNoTracking().SingleAsync(row => row.TargetUserId == manager.Id);
        audit.OldRole.Should().Be("Manager");
        audit.NewRole.Should().Be("Client");
        audit.ActorUserId.Should().Be(admin.Id);
    }

    [Fact]
    public async Task ChangeRole_SelfDowngradeAndLastAdmin_Return400()
    {
        var admin = await SeedAsync("only-admin@example.com", UserRole.Admin, firstName: "Единственный", lastName: "Админ");

        var self = await SendAsync(
            HttpMethod.Put,
            $"/identity/admin/users/{admin.Id}/role",
            admin.Id,
            AppRoles.Admin,
            new { role = "Manager" });
        self.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await DetailAsync(self)).Should().Contain(UserAdminText.SelfDowngrade);

        var second = await SeedAsync("second-admin@example.com", UserRole.Admin);
        var demoteSecond = await SendAsync(HttpMethod.Put, $"/identity/admin/users/{second.Id}/role", admin.Id, AppRoles.Admin, new { role = "Client" });
        demoteSecond.StatusCode.Should().Be(HttpStatusCode.OK);

        var last = await SendAsync(HttpMethod.Post, $"/identity/admin/users/{admin.Id}/block", second.Id, AppRoles.Admin, new { reason = "проверка" });
        // Второй админ уже клиент: токен ещё со старой ролью, но версия сессии выросла.
        last.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BlockAndUnblock_LoginMessageSessionsAndAudit()
    {
        await SetSupportPhoneAsync(Phone);
        var admin = await SeedAsync("admin-block@example.com", UserRole.Admin, firstName: "Админ", lastName: "Блока");
        var client = await SeedAsync("client-block@example.com", UserRole.Client, firstName: "Иван", lastName: "Блокируемый");
        var login = await LoginAsync(client.Email);
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokensDto>();

        var blocked = await SendAsync(
            HttpMethod.Post,
            $"/identity/admin/users/{client.Id}/block",
            admin.Id,
            AppRoles.Admin,
            new { reason = "нарушение правил" });
        blocked.StatusCode.Should().Be(HttpStatusCode.OK);

        var repeat = await SendAsync(
            HttpMethod.Post,
            $"/identity/admin/users/{client.Id}/block",
            admin.Id,
            AppRoles.Admin,
            new { reason = "ещё раз" });
        repeat.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await DetailAsync(repeat)).Should().Contain(UserAdminText.AlreadyBlocked);

        (await SendBearerAsync(HttpMethod.Get, "/identity/me", tokens!.AccessToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await _client.PostAsJsonAsync("/identity/refresh", new { refreshToken = tokens.RefreshToken })).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);

        var blockedLogin = await LoginAsync(client.Email);
        blockedLogin.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var blockedJson = await blockedLogin.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(blockedJson))
        {
            doc.RootElement.GetProperty("detail").GetString().Should().Be(AccountAccessText.Blocked(Phone));
            doc.RootElement.GetProperty("code").GetString().Should().Be("AccountBlocked");
        }

        var card = await SendAsync(HttpMethod.Get, $"/identity/admin/users/{client.Id}", admin.Id, AppRoles.Admin);
        var details = await card.Content.ReadFromJsonAsync<AdminUserDetailsDto>();
        details!.User.Status.Should().Be("Blocked");
        details.User.BlockReason.Should().Be("нарушение правил");
        details.Audit.Should().ContainSingle();
        details.Audit[0].ActorName.Should().Be("Админ Блока");
        details.Audit[0].Action.Should().Be(UserAdminAudit.ActionBlocked);

        var unblocked = await SendAsync(HttpMethod.Post, $"/identity/admin/users/{client.Id}/unblock", admin.Id, AppRoles.Admin);
        unblocked.StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync("/identity/refresh", new { refreshToken = tokens.RefreshToken })).StatusCode
            .Should().Be(HttpStatusCode.Unauthorized);
        (await LoginAsync(client.Email)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var audits = await db.UserAdminAudits.AsNoTracking().Where(row => row.TargetUserId == client.Id).ToListAsync();
        audits.Should().HaveCount(2);
    }

    [Fact]
    public async Task SelfBlock_Returns400()
    {
        var admin = await SeedAsync("admin-self@example.com", UserRole.Admin);
        var response = await SendAsync(
            HttpMethod.Post,
            $"/identity/admin/users/{admin.Id}/block",
            admin.Id,
            AppRoles.Admin,
            new { reason = "себя" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await DetailAsync(response)).Should().Contain(UserAdminText.SelfBlock);
    }

    [Fact]
    public async Task ParallelDemote_LeavesOneActiveAdmin()
    {
        ParallelAdminDemote.Reset();
        try
        {
            var first = await SeedAsync("admin-a@example.com", UserRole.Admin);
            var second = await SeedAsync("admin-b@example.com", UserRole.Admin);

            var left = PutRoleAsync(first.Id, second.Id);
            var right = PutRoleAsync(second.Id, first.Id);
            var responses = await Task.WhenAll(left, right);
            var details = await Task.WhenAll(responses.Select(DetailAsync));

            responses.Select(response => response.StatusCode)
                .Should()
                .BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.BadRequest]);
            details.Should().Contain(text => text.Contains(UserAdminText.RoleChanged, StringComparison.Ordinal));
            details.Should().Contain(text => text.Contains(UserAdminText.LastAdmin, StringComparison.Ordinal));
            details.Should().NotContain(text => text.Contains("A database error occurred.", StringComparison.Ordinal));

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var active = await db.Users.CountAsync(user => user.Role == UserRole.Admin && user.BlockedAt == null);
            active.Should().Be(1);
        }
        finally
        {
            ParallelAdminDemote.Reset();
        }
    }

    private static async Task<string> DetailAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("detail", out var detail)
            ? detail.GetString() ?? json
            : json;
    }

    private async Task<HttpResponseMessage> PutRoleAsync(Guid actorId, Guid targetId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/identity/admin/users/{targetId}/role");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(actorId, AppRoles.Admin));
        request.Headers.TryAddWithoutValidation(DemoteRaceLock.HeaderName, "1");
        request.Content = JsonContent.Create(new { role = "Client" });
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> LoginAsync(string email) =>
        await _client.PostAsJsonAsync("/identity/login", new { email, password = Password });

    private async Task<HttpResponseMessage> SendBearerAsync(HttpMethod method, string url, string accessToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, Guid userId, string role, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token(userId, role));
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private static string Token(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
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

    private async Task<User> SeedAsync(
        string email,
        UserRole role,
        string firstName = "Имя",
        string lastName = "Фамилия",
        string? phone = null,
        string? passwordHash = null)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hash = passwordHash ?? new PasswordHasherService().Hash(Password);
        var user = User.Create(email, hash, firstName, lastName, phone, role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task SetSupportPhoneAsync(string phone)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var row = await db.SiteSettings.SingleAsync();
        row.SupportPhone = phone;
        await db.SaveChangesAsync();
    }

    private static async Task ResetAsync(IdentityDbContext db)
    {
        db.UserAdminAudits.RemoveRange(await db.UserAdminAudits.ToListAsync());
        db.RefreshTokens.RemoveRange(await db.RefreshTokens.ToListAsync());
        db.Users.RemoveRange(await db.Users.ToListAsync());
        var settings = await db.SiteSettings.SingleOrDefaultAsync();
        if (settings is null)
            db.SiteSettings.Add(SiteSettings.CreateDefault(SiteSettings.SeedUpdatedAt));
        else
            settings.SupportPhone = null;
        await db.SaveChangesAsync();
    }
}
