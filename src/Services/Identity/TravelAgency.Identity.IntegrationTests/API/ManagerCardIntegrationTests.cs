using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.IntegrationTests.API;

public class ManagerCardIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC";
    private const string AboutText = "  Первая строка\nвторая  ";
    private const string AboutTrimmed = "Первая строка\nвторая";
    private const string TargetEmail = "marina.private@example.com";
    private const string TargetPhone = "+79995550001";
    private const string TargetCity = "Сочи";
    private const string TargetSpecialization = "Море-104";
    private const string BlockReason = "Секретная причина блокировки 104";

    private static readonly string[] ForbiddenKeys =
    [
        "email", "phone", "city", "specialization", "role", "firstName", "lastName",
        "createdAt", "notes", "blockedAt", "blockReason", "blockedByUserId", "isBlocked"
    ];

    private readonly CustomWebApplicationFactory _factory;
    private readonly Guid _avatarId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public ManagerCardIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Users.RemoveRange(db.Users);
        db.RefreshTokens.RemoveRange(db.RefreshTokens);
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Get_WithoutToken_Returns401WithoutCard()
    {
        var user = await SeedStaffAsync(UserRole.Manager, TargetEmail, FillFullCard);

        var response = await _factory.CreateClient().GetAsync(Path(user.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("displayName");
        body.Should().NotContain(AboutTrimmed);
        body.Should().NotContain(TargetEmail);
    }

    [Fact]
    public async Task Get_ForManagerAndAdmin_ReturnsSamePublicCard()
    {
        var manager = await SeedStaffAsync(UserRole.Manager, TargetEmail, FillFullCard);
        var admin = await SeedStaffAsync(UserRole.Admin, "admin.private@example.com", FillFullCard);
        var viewers = new[] { "Client", "Manager", "Admin" };

        foreach (var target in new[] { manager, admin })
        {
            string? sameViewerBody = null;
            foreach (var role in viewers)
            {
                var response = await ClientFor(Guid.NewGuid(), role).GetAsync(Path(target.Id));

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                HeaderValue(response, "Cache-Control").Should().Contain("no-store");

                var json = await response.Content.ReadAsStringAsync();
                sameViewerBody ??= json;
                json.Should().Be(sameViewerBody);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                root.GetProperty("id").GetGuid().Should().Be(target.Id);
                root.GetProperty("displayName").GetString().Should().Be("Марина Иванова");
                root.GetProperty("avatarUrl").GetString().Should().Be(ProfileMapper.BuildAvatarUrl(_avatarId));
                root.GetProperty("about").GetString().Should().Be(AboutTrimmed);
                root.GetProperty("tourismSince").GetInt32().Should().Be(2018);
                var languages = root.GetProperty("languages").EnumerateArray().Select(item => item.GetString()).ToArray();
                languages.Should().Equal("ru", "en");

                foreach (var key in ForbiddenKeys)
                    root.TryGetProperty(key, out _).Should().BeFalse();

                json.Should().NotContain(TargetEmail);
                json.Should().NotContain("admin.private@example.com");
                json.Should().NotContain(TargetPhone);
                json.Should().NotContain(TargetCity);
                json.Should().NotContain(TargetSpecialization);
            }
        }
    }

    [Fact]
    public async Task Get_OmitsEmptyOptionalFields()
    {
        var whitespace = await SeedStaffAsync(UserRole.Manager, "blank.ws@example.com", user =>
        {
            user.UpdateProfile("Марина", "Иванова", null, null);
            user.UpdateManagerProfile("  \n  ", null, [], null);
        });
        var nulls = await SeedStaffAsync(UserRole.Manager, "blank.null@example.com", user =>
        {
            user.UpdateProfile("Марина", "Иванова", null, null);
            user.UpdateManagerProfile(null, null, null, null);
        });
        var emptyAbout = await SeedStaffAsync(UserRole.Manager, "blank.empty@example.com", user =>
        {
            user.UpdateProfile("Марина", "Иванова", null, null);
            user.UpdateManagerProfile("", null, null, null);
        });

        foreach (var user in new[] { whitespace, nulls, emptyAbout })
        {
            var response = await ClientFor(Guid.NewGuid(), "Client").GetAsync(Path(user.Id));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            root.GetProperty("id").GetGuid().Should().Be(user.Id);
            root.GetProperty("displayName").GetString().Should().Be("Марина Иванова");
            foreach (var key in new[] { "about", "tourismSince", "languages", "avatarUrl" })
                root.TryGetProperty(key, out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Get_UnknownAndClient_ReturnSameNotFound_OwnStaffReturnsCard()
    {
        var client = await SeedStaffAsync(UserRole.Client, "client.secret@example.com", user =>
            user.UpdateProfile("Клиент", "Скрытый", "+79990000002", "Казань"));
        var manager = await SeedStaffAsync(UserRole.Manager, "own.manager@example.com", FillFullCard);
        var admin = await SeedStaffAsync(UserRole.Admin, "own.admin@example.com", FillFullCard);
        var missingId = Guid.NewGuid();

        var missing = await ReadNotFoundAsync(missingId, Guid.NewGuid());
        var foreignClient = await ReadNotFoundAsync(client.Id, Guid.NewGuid());
        var ownClient = await ReadNotFoundAsync(client.Id, client.Id);

        missing.Should().Be("User not found.");
        foreignClient.Should().Be("User not found.");
        ownClient.Should().Be("User not found.");

        var ownManager = await ClientFor(manager.Id, "Manager").GetAsync(Path(manager.Id));
        var ownAdmin = await ClientFor(admin.Id, "Admin").GetAsync(Path(admin.Id));
        ownManager.StatusCode.Should().Be(HttpStatusCode.OK);
        ownAdmin.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ownManager.Content.ReadAsStringAsync()).Should().Contain(manager.Id.ToString());
        (await ownAdmin.Content.ReadAsStringAsync()).Should().Contain(admin.Id.ToString());
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("abc")]
    public async Task Get_NonGuid_DoesNotReachCardAction(string segment)
    {
        var response = await ClientFor(Guid.NewGuid(), "Client")
            .GetAsync($"/identity/users/{segment}/manager-card");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("displayName");
        body.Should().NotContain("\"id\"");
    }

    [Fact]
    public async Task Get_BlockedStaff_ReturnsCardWithoutBlockFields()
    {
        var manager = await SeedStaffAsync(UserRole.Manager, "blocked.manager@example.com", user =>
        {
            FillFullCard(user);
            user.Block(BlockReason, Guid.NewGuid(), DateTime.UtcNow);
        });
        var admin = await SeedStaffAsync(UserRole.Admin, "blocked.admin@example.com", user =>
        {
            FillFullCard(user);
            user.Block(BlockReason, Guid.NewGuid(), DateTime.UtcNow);
        });

        foreach (var user in new[] { manager, admin })
        {
            var response = await ClientFor(Guid.NewGuid(), "Client").GetAsync(Path(user.Id));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            doc.RootElement.GetProperty("about").GetString().Should().Be(AboutTrimmed);
            json.Should().NotContain(BlockReason);
            foreach (var key in new[] { "blockedAt", "blockReason", "blockedByUserId", "isBlocked" })
                doc.RootElement.TryGetProperty(key, out _).Should().BeFalse();
        }
    }

    [Fact]
    public async Task Get_DoesNotLogCardBody()
    {
        var user = await SeedStaffAsync(UserRole.Manager, TargetEmail, FillFullCard);
        _factory.Logs.Clear();
        _factory.Logs.Capture = true;
        try
        {
            var response = await ClientFor(Guid.NewGuid(), "Client").GetAsync(Path(user.Id));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            _factory.Logs.Capture = false;
        }

        var log = string.Join('\n', _factory.Logs.Lines);
        log.Should().Contain("manager-card");
        log.Should().NotContain(AboutTrimmed);
        log.Should().NotContain("Первая строка");
        log.Should().NotContain(TargetEmail);
        log.Should().NotContain(TargetPhone);
    }

    private async Task<string> ReadNotFoundAsync(Guid targetId, Guid viewerId)
    {
        var response = await ClientFor(viewerId, "Client").GetAsync(Path(targetId));
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotContain("client.secret@example.com");
        json.Should().NotContain("+79990000002");
        json.Should().NotContain("Казань");
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("email", out _).Should().BeFalse();
        doc.RootElement.TryGetProperty("phone", out _).Should().BeFalse();
        doc.RootElement.TryGetProperty("role", out _).Should().BeFalse();
        return doc.RootElement.GetProperty("detail").GetString()!;
    }

    private void FillFullCard(User user)
    {
        user.UpdateProfile("Марина", "Иванова", TargetPhone, TargetCity);
        user.UpdateManagerProfile(AboutText, 2018, ["ru", "en"], TargetSpecialization);
        user.SetAvatar(_avatarId);
    }

    private async Task<User> SeedStaffAsync(UserRole role, string email, Action<User> configure)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = new PasswordHasherService();
        var user = User.Create(email, hasher.Hash("Password1"), "Марина", "Иванова", null, role);
        configure(user);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private HttpClient ClientFor(Guid userId, string role)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestJwt(userId, "viewer@example.com", role));
        return client;
    }

    private static string Path(Guid id) => $"/identity/users/{id}/manager-card";

    private static string HeaderValue(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
            return string.Join(", ", values);
        if (response.Content.Headers.TryGetValues(name, out var contentValues))
            return string.Join(", ", contentValues);
        return string.Empty;
    }

    private static string GenerateTestJwt(Guid userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
