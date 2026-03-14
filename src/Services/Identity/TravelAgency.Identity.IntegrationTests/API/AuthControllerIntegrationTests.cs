using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.IntegrationTests.API;

public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private const string SigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC";
    private const string Issuer = "TestIssuer";
    private const string Audience = "TestAudience";

    public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
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

    private string GenerateTestJwt(Guid userId, string email, string role = "Client")
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

    private async Task<User> SeedUserAsync(string email = "test@example.com", string password = "Password1")
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = new PasswordHasherService();
        var user = User.Create(email, hasher.Hash(password), "Test", "User", null);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task Register_WithValidRequest_Returns201()
    {
        var request = new RegisterRequest("newuser@example.com", "Password1", "New", "User", null);

        var response = await _client.PostAsJsonAsync("/identity/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        await SeedUserAsync("dup@example.com");
        var request = new RegisterRequest("dup@example.com", "Password1", "Dup", "User", null);

        var response = await _client.PostAsJsonAsync("/identity/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WithInvalidRequest_Returns400()
    {
        var request = new RegisterRequest("not-an-email", "weak", "", "", null);

        var response = await _client.PostAsJsonAsync("/identity/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200()
    {
        await SeedUserAsync("login@example.com", "Password1");
        var request = new LoginRequest("login@example.com", "Password1");

        var response = await _client.PostAsJsonAsync("/identity/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await SeedUserAsync("loginwrong@example.com", "Password1");
        var request = new LoginRequest("loginwrong@example.com", "WrongPassword1");

        var response = await _client.PostAsJsonAsync("/identity/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var request = new LoginRequest("unknown@example.com", "Password1");

        var response = await _client.PostAsJsonAsync("/identity/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithValidToken_Returns204()
    {
        await SeedUserAsync("logout@example.com", "Password1");
        var loginResp = await _client.PostAsJsonAsync("/identity/login", new LoginRequest("logout@example.com", "Password1"));
        var tokens = await loginResp.Content.ReadFromJsonAsync<AuthTokensDto>();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/identity/logout");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        request.Content = JsonContent.Create(new RefreshTokenRequest(tokens.RefreshToken));
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_WithValidToken_Returns200()
    {
        await SeedUserAsync("refresh@example.com", "Password1");
        var loginResp = await _client.PostAsJsonAsync("/identity/login", new LoginRequest("refresh@example.com", "Password1"));
        var tokens = await loginResp.Content.ReadFromJsonAsync<AuthTokensDto>();
        var refreshRequest = new RefreshTokenRequest(tokens!.RefreshToken);

        var response = await _client.PostAsJsonAsync("/identity/refresh", refreshRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newTokens = await response.Content.ReadFromJsonAsync<AuthTokensDto>();
        newTokens.Should().NotBeNull();
        newTokens!.AccessToken.Should().NotBeNullOrEmpty();
        newTokens.RefreshToken.Should().NotBe(tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_Returns401()
    {
        var request = new RefreshTokenRequest("invalid-or-nonexistent-token");

        var response = await _client.PostAsJsonAsync("/identity/refresh", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_WithoutAuthentication_Returns401()
    {
        var response = await _client.GetAsync("/identity/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_WithAuthentication_Returns200()
    {
        var user = await SeedUserAsync("me@example.com");
        var jwt = GenerateTestJwt(user.Id, user.Email, user.Role.ToRoleString());
        using var request = new HttpRequestMessage(HttpMethod.Get, "/identity/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.Email.Should().Be("me@example.com");
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuthentication_Returns401()
    {
        var updateRequest = new UpdateProfileRequest("New", "Name", null);

        var response = await _client.PatchAsJsonAsync("/identity/me", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WithAuthentication_Returns200()
    {
        var user = await SeedUserAsync("update@example.com");
        var jwt = GenerateTestJwt(user.Id, user.Email, user.Role.ToRoleString());

        using var request = new HttpRequestMessage(HttpMethod.Patch, "/identity/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        request.Content = JsonContent.Create(new UpdateProfileRequest("Updated", "Name", null));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        profile.Should().NotBeNull();
        profile!.FirstName.Should().Be("Updated");
    }
}
