using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.IntegrationTests.API;

public class ProfileAvatarIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private const string SigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    public ProfileAvatarIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.EnsureCreatedAsync();
        _factory.AvatarMedia.Uploaded.Clear();
        _factory.AvatarMedia.Deleted.Clear();
        _factory.AvatarMedia.UploadError = null;
        _factory.AvatarMedia.DeleteError = null;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task PatchMe_ClientWithAbout_Returns400WithField()
    {
        var user = await SeedUserAsync("client-about@example.com", role: UserRole.Client);
        var jwt = GenerateTestJwt(user.Id, user.Email, "Client");

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Content = JsonContent.Create(new { firstName = "Иван", about = "нельзя" });

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("about");
    }

    [Fact]
    public async Task PatchMe_ManagerProfileFields_Returns200()
    {
        var user = await SeedUserAsync("mgr-profile@example.com", role: UserRole.Manager);
        var jwt = GenerateTestJwt(user.Id, user.Email, "Manager");

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Content = JsonContent.Create(new
        {
            city = "Казань",
            about = "Гид",
            tourismSince = 2015,
            languages = new[] { "ru", "en" },
            specialization = "Кавказ"
        });

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile!.City.Should().Be("Казань");
        profile.About.Should().Be("Гид");
        profile.TourismSince.Should().Be(2015);
        profile.Languages.Should().BeEquivalentTo(["ru", "en"]);
        profile.Specialization.Should().Be("Кавказ");
        profile.AvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task PatchMe_WithEmailField_Returns400()
    {
        var user = await SeedUserAsync("email-patch@example.com");
        var jwt = GenerateTestJwt(user.Id, user.Email, "Client");

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/identity/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        request.Content = new StringContent(
            """{"firstName":"A","email":"hacked@example.com"}""",
            Encoding.UTF8,
            "application/json");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostAvatar_SetsAvatarUrl_AndReplaceDeletesOld()
    {
        var user = await SeedUserAsync("avatar@example.com");
        var jwt = GenerateTestJwt(user.Id, user.Email, "Client");
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        _factory.AvatarMedia.NextId = firstId;

        var response1 = await SendAvatarAsync(jwt, "a.png");
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile1 = await response1.Content.ReadFromJsonAsync<UserProfileDto>();
        profile1!.AvatarUrl.Should().Be($"/api/v1/media/files/{firstId:D}/a256");

        _factory.AvatarMedia.NextId = secondId;
        var response2 = await SendAvatarAsync(jwt, "b.png");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile2 = await response2.Content.ReadFromJsonAsync<UserProfileDto>();
        profile2!.AvatarUrl.Should().Be($"/api/v1/media/files/{secondId:D}/a256");
        _factory.AvatarMedia.Deleted.Should().Contain(firstId);

        using var del = new HttpRequestMessage(HttpMethod.Delete, "/identity/me/avatar");
        del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var delResp = await _client.SendAsync(del);
        delResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cleared = await delResp.Content.ReadFromJsonAsync<UserProfileDto>();
        cleared!.AvatarUrl.Should().BeNull();
        _factory.AvatarMedia.Deleted.Should().Contain(secondId);
    }

    [Fact]
    public async Task PostAvatar_WhenMediaForbids_Returns403()
    {
        var user = await SeedUserAsync("avatar-forbid@example.com");
        var jwt = GenerateTestJwt(user.Id, user.Email, "Client");
        _factory.AvatarMedia.UploadError = new ForbiddenException("Нельзя изменить аватар другого пользователя.");

        var response = await SendAvatarAsync(jwt, "a.png");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        _factory.AvatarMedia.UploadError = null;
    }

    private async Task<HttpResponseMessage> SendAvatarAsync(string jwt, string fileName)
    {
        using var content = new MultipartFormDataContent();
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/identity/me/avatar") { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return await _client.SendAsync(request);
    }

    private async Task<User> SeedUserAsync(string email, string password = "Password1", UserRole role = UserRole.Client)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = new PasswordHasherService();
        var user = User.Create(email, hasher.Hash(password), "Test", "User", null, role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
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
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
