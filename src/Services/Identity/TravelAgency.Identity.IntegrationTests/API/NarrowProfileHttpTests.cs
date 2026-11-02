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
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.IntegrationTests.API;

public class NarrowProfileHttpTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private const string SigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC";
    private const string NameError = "Укажите имя длиной от 1 до 100 символов.";
    private const string LastNameError = "Укажите фамилию длиной от 1 до 100 символов.";
    private const string PhoneError = "Телефон должен быть в формате E.164, например +79991234567, или пустым.";

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NarrowProfileHttpTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(UserRole.Client)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task GetAndPatch_KeepTheNarrowProfile(UserRole role)
    {
        var user = await SeedAsync($"narrow-{role}@example.com", role, "+79990000001");
        var jwt = Jwt(user.Id, user.Email, role.ToRoleString());

        var initial = await SendAsync(HttpMethod.Get, "/identity/me", jwt);
        initial.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertNarrow(await initial.Content.ReadAsStringAsync(), user, "Test", "User", "+79990000001");

        var omitted = await SendAsync(HttpMethod.Patch, "/identity/me", jwt, new { });
        omitted.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReloadAsync(user.Id)).FirstName.Should().Be("Test");

        var nulled = await SendAsync(HttpMethod.Patch, "/identity/me", jwt, new { firstName = (string?)null, phone = (string?)null });
        nulled.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReloadAsync(user.Id)).Phone.Should().Be("+79990000001");

        var cleared = await SendAsync(HttpMethod.Patch, "/identity/me", jwt, new { phone = "" });
        cleared.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertNarrow(await cleared.Content.ReadAsStringAsync(), user, "Test", "User", null);
        (await ReloadAsync(user.Id)).Phone.Should().BeNull();

        var trimmed = await SendAsync(
            HttpMethod.Patch,
            "/identity/me",
            jwt,
            new { firstName = " Марина ", lastName = " Иванова ", phone = " +79991234567 " });
        trimmed.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertNarrow(await trimmed.Content.ReadAsStringAsync(), user, "Марина", "Иванова", "+79991234567");
        var stored = await ReloadAsync(user.Id);
        stored.FirstName.Should().Be("Марина");
        stored.LastName.Should().Be("Иванова");
        stored.Phone.Should().Be("+79991234567");
        stored.Email.Should().Be(user.Email);
        stored.Role.Should().Be(role);

        var again = await SendAsync(HttpMethod.Get, "/identity/me", jwt);
        AssertNarrow(await again.Content.ReadAsStringAsync(), user, "Марина", "Иванова", "+79991234567");
    }

    [Theory]
    [InlineData("", null, NameError, null)]
    [InlineData("   ", null, NameError, null)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", null, NameError, null)]
    [InlineData(null, "", null, LastNameError)]
    [InlineData(null, "   ", null, LastNameError)]
    [InlineData("", " ", NameError, LastNameError)]
    public async Task Patch_InvalidNames_Return400AndKeepTheRow(
        string? firstName,
        string? lastName,
        string? firstError,
        string? lastError)
    {
        var user = await SeedAsync($"bad-name-{Guid.NewGuid():N}@example.com", UserRole.Client, "+79990000001");
        var response = await SendAsync(
            HttpMethod.Patch,
            "/identity/me",
            Jwt(user.Id, user.Email, "Client"),
            new { firstName, lastName });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
        if (firstError is not null)
            doc.RootElement.GetProperty("errors").GetProperty("firstName")[0].GetString().Should().Be(firstError);
        if (lastError is not null)
            doc.RootElement.GetProperty("errors").GetProperty("lastName")[0].GetString().Should().Be(lastError);
        var stored = await ReloadAsync(user.Id);
        stored.FirstName.Should().Be("Test");
        stored.Phone.Should().Be("+79990000001");
    }

    [Theory]
    [InlineData("89991234567")]
    [InlineData("+1")]
    [InlineData("   ")]
    public async Task Patch_InvalidPhone_Returns400AndKeepsTheRow(string phone)
    {
        var user = await SeedAsync($"bad-phone-{Guid.NewGuid():N}@example.com", UserRole.Manager, "+79990000001");
        var response = await SendAsync(
            HttpMethod.Patch,
            "/identity/me",
            Jwt(user.Id, user.Email, "Manager"),
            new { phone });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
        doc.RootElement.GetProperty("errors").GetProperty("phone")[0].GetString().Should().Be(PhoneError);
        (await ReloadAsync(user.Id)).Phone.Should().Be("+79990000001");
    }

    [Theory]
    [InlineData("А")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Patch_NameBoundaries_Return200(string name)
    {
        var user = await SeedAsync($"edge-{Guid.NewGuid():N}@example.com", UserRole.Admin, null);
        var response = await SendAsync(
            HttpMethod.Patch,
            "/identity/me",
            Jwt(user.Id, user.Email, "Admin"),
            new { firstName = " " + name + " ", lastName = " " + name + " ", phone = "+12" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stored = await ReloadAsync(user.Id);
        stored.FirstName.Should().Be(name);
        stored.LastName.Should().Be(name);
        stored.Phone.Should().Be("+12");
    }

    [Theory]
    [InlineData("email")]
    [InlineData("role")]
    [InlineData("city")]
    [InlineData("about")]
    [InlineData("tourismSince")]
    [InlineData("languages")]
    [InlineData("specialization")]
    [InlineData("passport")]
    public async Task Patch_UnknownMember_Returns400(string key)
    {
        var user = await SeedAsync($"unknown-{key}-{Guid.NewGuid():N}@example.com", UserRole.Client, "+79990000001");
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Jwt(user.Id, user.Email, "Client"));
        request.Content = new StringContent(
            $$"""{"firstName":"Новое","{{key}}":"лишнее"}""",
            Encoding.UTF8,
            "application/json");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain("доступно только менеджеру или администратору");
        (await ReloadAsync(user.Id)).FirstName.Should().Be("Test");
    }

    [Fact]
    public async Task Me_GuestIs401_AndMissingRowIs404()
    {
        var guest = await _client.GetAsync("/identity/me");
        guest.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await guest.Content.ReadAsStringAsync()).Should().NotContain("firstName");

        var missing = await SendAsync(HttpMethod.Get, "/identity/me", Jwt(Guid.NewGuid(), "gone@example.com", "Client"));
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var doc = JsonDocument.Parse(await missing.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("detail").GetString().Should().Be("User not found.");
    }

    private static void AssertNarrow(string json, User user, string firstName, string lastName, string? phone)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("id").GetGuid().Should().Be(user.Id);
        root.GetProperty("email").GetString().Should().Be(user.Email);
        root.GetProperty("firstName").GetString().Should().Be(firstName);
        root.GetProperty("lastName").GetString().Should().Be(lastName);
        root.GetProperty("role").GetString().Should().Be(user.Role.ToRoleString());
        root.TryGetProperty("createdAt", out _).Should().BeTrue();
        root.TryGetProperty("avatarUrl", out var avatar).Should().BeTrue();
        avatar.ValueKind.Should().Be(JsonValueKind.Null);
        if (phone is null)
            root.GetProperty("phone").ValueKind.Should().Be(JsonValueKind.Null);
        else
            root.GetProperty("phone").GetString().Should().Be(phone);
        foreach (var key in new[] { "city", "about", "tourismSince", "languages", "specialization" })
            root.TryGetProperty(key, out _).Should().BeFalse();
    }

    private async Task<User> ReloadAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.Users.AsNoTracking().SingleAsync(user => user.Id == id);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string jwt, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private async Task<User> SeedAsync(string email, UserRole role, string? phone)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var user = User.Create(email, new PasswordHasherService().Hash("Password1"), "Test", "User", phone, role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static string Jwt(Guid userId, string email, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey));
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
