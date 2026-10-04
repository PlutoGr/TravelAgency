using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Shared.Infrastructure.Tests.Extensions;

/// <summary>
/// Tests for AddJwtAuthentication shared extension (AUDIT-007).
/// </summary>
public class JwtAuthenticationExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void AddJwtAuthentication_WhenSigningKeyIsAbsent_ThrowsWithExpectedMessage()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "issuer",
            ["JwtSettings:Audience"] = "audience",
        });

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddJwtAuthentication(config));

        ex.Message.Should().Contain("JWT_SIGNING_KEY");
        ex.Message.Should().Contain("SigningKey");
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("under-32-chars-1234567")]
    public void AddJwtAuthentication_WhenSigningKeyIsTooShort_ThrowsWithExpectedMessage(string shortKey)
    {
        var services = new ServiceCollection();
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:SigningKey"] = shortKey,
            ["JwtSettings:Issuer"] = "issuer",
            ["JwtSettings:Audience"] = "audience",
        });

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddJwtAuthentication(config));

        // Empty/whitespace triggers "must be configured"; non-empty but short triggers "at least 32 characters"
        (ex.Message.Contains("JWT SigningKey must be at least 32 characters") ||
         ex.Message.Contains("JWT SigningKey must be configured"))
            .Should().BeTrue($"Expected message about invalid signing key, got: {ex.Message}");
    }

    [Fact]
    public void AddJwtAuthentication_WhenSigningKeyProvidedViaJwtSigningKeyConfiguration_Succeeds()
    {
        var services = new ServiceCollection();
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["JWT_SIGNING_KEY"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
            ["JwtSettings:Issuer"] = "issuer",
            ["JwtSettings:Audience"] = "audience",
        });

        var ex = Record.Exception(() => services.AddJwtAuthentication(config));
        ex.Should().BeNull();
    }

    [Fact]
    public void AddJwtAuthentication_WhenSigningKeyIsExactly32Chars_DoesNotThrow()
    {
        var services = new ServiceCollection();
        var exactly32 = new string('x', 32);
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:SigningKey"] = exactly32,
            ["JwtSettings:Issuer"] = "issuer",
            ["JwtSettings:Audience"] = "audience",
        });

        var ex = Record.Exception(() => services.AddJwtAuthentication(config));
        ex.Should().BeNull();
    }

    [Fact]
    public void AddJwtAuthentication_WhenSigningKeyIsValid_RegistersJwtBearerAuthentication()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:SigningKey"] = "a-valid-signing-key-at-least-32-chars!",
            ["JwtSettings:Issuer"] = "test-issuer",
            ["JwtSettings:Audience"] = "test-audience",
        });

        services.AddJwtAuthentication(config);
        var provider = services.BuildServiceProvider();

        var authService = provider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        authService.Should().NotBeNull();
    }
}
