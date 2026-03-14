using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.UnitTests.Infrastructure.Services;

public class JwtTokenServiceTests
{
    private readonly JwtSettings _settings = new()
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        SigningKey = "ThisIsATestSigningKeyWithAtLeast32Chars!",
        AccessTokenExpirationMinutes = 60,
        RefreshTokenExpirationDays = 7,
        ValidateLifetime = true
    };

    private readonly JwtTokenService _service;

    public JwtTokenServiceTests()
    {
        _service = new JwtTokenService(Options.Create(_settings));
    }

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyAccessToken()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null, "Client");

        var result = _service.GenerateAccessToken(user);

        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAccessToken_RefreshTokenIsEmpty()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);

        var result = _service.GenerateAccessToken(user);

        result.RefreshToken.Should().BeEmpty();
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAtIsInFuture()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);

        var result = _service.GenerateAccessToken(user);

        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresAtMatchesConfiguredMinutes()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);
        var before = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes - 1);

        var result = _service.GenerateAccessToken(user);

        result.ExpiresAt.Should().BeAfter(before);
        result.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes + 1));
    }

    [Fact]
    public void GenerateAccessToken_TokenContainsNameIdentifierClaim()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);

        var result = _service.GenerateAccessToken(user);

        var claims = ParseClaims(result.AccessToken);
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
    }

    [Fact]
    public void GenerateAccessToken_TokenContainsEmailClaim()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);

        var result = _service.GenerateAccessToken(user);

        var claims = ParseClaims(result.AccessToken);
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "test@example.com");
    }

    [Fact]
    public void GenerateAccessToken_TokenContainsRoleClaim()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null, "Manager");

        var result = _service.GenerateAccessToken(user);

        var claims = ParseClaims(result.AccessToken);
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Manager");
    }

    [Fact]
    public void GenerateAccessToken_TokenContainsJtiClaim()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);

        var result = _service.GenerateAccessToken(user);

        var claims = ParseClaims(result.AccessToken);
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateAccessToken_TwoCallsProduceDifferentJti()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);

        var result1 = _service.GenerateAccessToken(user);
        var result2 = _service.GenerateAccessToken(user);

        var jti1 = ParseClaims(result1.AccessToken).First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = ParseClaims(result2.AccessToken).First(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        jti1.Should().NotBe(jti2);
    }

    [Fact]
    public void GenerateAccessToken_TokenIsValidWithCorrectKey()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);

        var result = _service.GenerateAccessToken(user);

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _settings.Issuer,
            ValidAudience = _settings.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };

        var act = () => tokenHandler.ValidateToken(result.AccessToken, validationParams, out _);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsNonEmptyString()
    {
        var token = _service.GenerateRefreshToken();

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64EncodedString()
    {
        var token = _service.GenerateRefreshToken();

        var act = () => Convert.FromBase64String(token);
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateRefreshToken_Returns64ByteBase64String()
    {
        var token = _service.GenerateRefreshToken();

        var bytes = Convert.FromBase64String(token);
        bytes.Should().HaveCount(64);
    }

    [Fact]
    public void GenerateRefreshToken_TwoCallsProduceDifferentTokens()
    {
        var token1 = _service.GenerateRefreshToken();
        var token2 = _service.GenerateRefreshToken();

        token1.Should().NotBe(token2);
    }

    private IEnumerable<Claim> ParseClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims;
    }
}
