using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Contracts.Grpc.Identity;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Identity.Infrastructure.Extensions;
using TravelAgency.Identity.Infrastructure.GrpcServices;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Services;
using TravelAgency.Shared.Contracts.Auth;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.IntegrationTests.API;

public class AccountLinksHttpTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC";
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly PasswordHasherService _hasher = new();

    public AccountLinksHttpTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        _factory.Mail.Reset();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.EmailChangeRequests.RemoveRange(db.EmailChangeRequests);
        db.PasswordResetTokens.RemoveRange(db.PasswordResetTokens);
        db.RefreshTokens.RemoveRange(db.RefreshTokens);
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Tk17_Contracts_Cache_Codes_AndEmptyUnauthorized()
    {
        var user = await SeedAsync("contract@example.com");
        var extra = await _client.PostAsJsonAsync("/identity/password-resets/request", new { email = "contract@example.com", extra = 1 });
        extra.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.Mail.Calls.Should().BeEmpty();

        var invalid = await _client.PostAsJsonAsync("/identity/password-resets/request", new { email = "not-an-email" });
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Code(invalid)).Should().Be("InvalidEmail");

        var neutral = await _client.PostAsJsonAsync("/identity/password-resets/request", new { email = "  Missing@Example.com " });
        neutral.StatusCode.Should().Be(HttpStatusCode.OK);
        neutral.Headers.CacheControl?.NoStore.Should().BeTrue();
        var neutralJson = await neutral.Content.ReadAsStringAsync();
        neutralJson.Should().Contain(AccountLinkMessages.PasswordResetRequested);
        neutralJson.Should().NotContain("accessToken");
        neutralJson.Should().NotContain("token=");

        var guestPassword = await _client.PostAsJsonAsync("/identity/me/password", new { currentPassword = "old-password", newPassword = "Password1", passwordConfirm = "Password1" });
        guestPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await guestPassword.Content.ReadAsStringAsync()).Should().BeEmpty();
        var guestEmail = await _client.PostAsJsonAsync("/identity/me/email", new { email = "next@example.com", currentPassword = "old-password" });
        guestEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await guestEmail.Content.ReadAsStringAsync()).Should().BeEmpty();

        var changed = await SendAsync(HttpMethod.Post, "/identity/me/password", Jwt(user), new
        {
            currentPassword = "old-password",
            newPassword = "Password1",
            passwordConfirm = "Password1"
        });
        changed.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await changed.Content.ReadFromJsonAsync<AuthTokensDto>();
        tokens!.AccessToken.Should().NotBeNullOrEmpty();
        tokens.RefreshToken.Should().NotBeNullOrEmpty();
        _factory.Mail.Calls.Should().NotContain(call => call.Subject == "Пароль изменён");

        var register = await _client.PostAsJsonAsync("/identity/register", new RegisterRequest("reg@example.com", "short", "Анна", "Смирнова", null));
        register.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await Code(register)).Should().Be("PasswordRule");
    }

    [Fact]
    public async Task Tk20_PublicLinksIgnoreRevokedCookie_EmailChangeSplitsBlockAndStaleSession()
    {
        var user = await SeedAsync("session@example.com");
        user.Block("причина", Guid.NewGuid(), DateTime.UtcNow);
        await SaveAsync(user);

        var blockedEmail = await SendAsync(HttpMethod.Post, "/identity/me/email", Jwt(user), new { email = "next@example.com", currentPassword = "wrong" });
        blockedEmail.StatusCode.Should().Be(HttpStatusCode.OK);
        (await blockedEmail.Content.ReadAsStringAsync()).Should().Contain(AccountLinkMessages.EmailChangeRequested);
        _factory.Mail.Calls.Should().NotContain(call => call.Deliver);

        var reset = await SendAsync(HttpMethod.Post, "/identity/password-resets/request", Jwt(user), new { email = user.Email });
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        user.Unblock(DateTime.UtcNow);
        user.RevokeSessions();
        await SaveAsync(user);
        var stalePassword = await SendAsync(HttpMethod.Post, "/identity/me/password", Jwt(user, version: 0), new
        {
            currentPassword = "old-password",
            newPassword = "Password1",
            passwordConfirm = "Password1"
        });
        stalePassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await stalePassword.Content.ReadAsStringAsync()).Should().BeEmpty();
        var reloaded = await ReloadAsync(user.Id);
        _hasher.Verify("old-password", reloaded.PasswordHash).Should().BeTrue();

        var staleEmail = await SendAsync(HttpMethod.Post, "/identity/me/email", Jwt(user, version: 0), new { email = "next@example.com", currentPassword = "old-password" });
        staleEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Tk22_Lockout_LoginAndSummaryFollowTheStoredEmail()
    {
        var user = await SeedAsync("lock@example.com");
        for (var attempt = 0; attempt < 3; attempt++)
            (await _client.PostAsJsonAsync("/identity/login", new LoginRequest(user.Email, "wrong-password"))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        (await _client.PostAsJsonAsync("/identity/login", new LoginRequest(user.Email, "old-password"))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await _client.PostAsJsonAsync("/identity/password-resets/request", new { email = user.Email });
        (await _client.PostAsJsonAsync("/identity/login", new LoginRequest(user.Email, "old-password"))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var link = _factory.Mail.Calls.Single(call => call.Deliver && call.Subject == "Восстановление пароля").LinkUrl!;
        var secret = link.Split("token=", StringSplitOptions.None)[1];
        var set = await _client.PostAsJsonAsync("/identity/password-resets", new { token = secret, password = "Password1", passwordConfirm = "Password1" });
        set.StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync("/identity/login", new LoginRequest(user.Email, "Password1"))).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync("/identity/login", new LoginRequest(user.Email, "old-password"))).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var before = await SummaryAsync(user.Id);
        before.Email.Should().Be(user.Email);
        var confirm = await SendAsync(HttpMethod.Post, "/identity/me/email", Jwt(await ReloadAsync(user.Id)), new { email = "next@example.com", currentPassword = "Password1" });
        confirm.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmLink = _factory.Mail.Calls.Single(call => call.Subject == "Подтвердите новый адрес").LinkUrl!;
        var confirmSecret = confirmLink.Split("token=", StringSplitOptions.None)[1];
        var stillOld = await SummaryAsync(user.Id);
        stillOld.Email.Should().Be(user.Email);
        var done = await _client.PostAsJsonAsync("/identity/email-confirmations", new { token = confirmSecret });
        done.StatusCode.Should().Be(HttpStatusCode.OK);
        (await SummaryAsync(user.Id)).Email.Should().Be("next@example.com");
    }

    [Fact]
    public void Tk23_MailQuotaUsesTheSameStoreAsLockout()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<IAccountMailQuota>().Should().BeOfType<InMemoryAccountMailQuota>();
            scope.ServiceProvider.GetRequiredService<ILockoutService>().Should().BeOfType<InMemoryLockoutService>();
        }

        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:IdentityDb"] = "Host=localhost;Database=identity;Username=u;Password=CHANGE_ME",
            ["ConnectionStrings:Redis"] = "127.0.0.1:6399"
        }).Build();
        services.AddIdentityInfrastructure(configuration);
        services.Single(descriptor => descriptor.ServiceType == typeof(IAccountMailQuota)).ImplementationType
            .Should().Be(typeof(RedisAccountMailQuota));
        services.Single(descriptor => descriptor.ServiceType == typeof(ILockoutService)).ImplementationType
            .Should().Be(typeof(RedisLockoutService));
    }

    private async Task<UserSummaryResponse> SummaryAsync(Guid userId)
    {
        using var scope = _factory.Services.CreateScope();
        var service = new IdentityGrpcService(scope.ServiceProvider.GetRequiredService<IUserRepository>());
        return await service.GetUserSummary(new GetUserSummaryRequest { UserId = userId.ToString() }, new SilentCall());
    }

    private async Task<User> SeedAsync(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = User.Create(email, _hasher.Hash("old-password"), "Анна", "Смирнова", null, UserRole.Client);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task SaveAsync(User user)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    private async Task<User> ReloadAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Users.AsNoTracking().SingleAsync(user => user.Id == id);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string jwt, object body)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private static async Task<string?> Code(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("code").GetString();
    }

    private static string Jwt(User user, long? version = null)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, "Client"),
            new Claim(SessionRevocationNames.ClaimType, (version ?? user.SessionVersion).ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class SilentCall : ServerCallContext
    {
        protected override string MethodCore => "test";
        protected override string HostCore => "test";
        protected override string PeerCore => "test";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => [];
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore =>
            new(null, new Dictionary<string, List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
