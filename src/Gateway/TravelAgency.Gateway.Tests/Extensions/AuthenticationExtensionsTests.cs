using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using TravelAgency.Gateway.Extensions;

namespace TravelAgency.Gateway.Tests.Extensions;

public class AuthenticationExtensionsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void AddGatewayAuthentication_WhenSigningKeyIsAbsent_ThrowsWithExpectedMessage()
    {
        // Arrange
        var services = new ServiceCollection();
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "issuer",
            ["JwtSettings:Audience"] = "audience",
        });

        // Act
        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddGatewayAuthentication(config));

        // Assert
        Assert.Contains(
            "JWT SigningKey must be configured via environment variable JwtSettings__SigningKey",
            ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("under-32-chars-1234567")]
    public void AddGatewayAuthentication_WhenSigningKeyIsTooShort_ThrowsWithExpectedMessage(string shortKey)
    {
        // Arrange
        var services = new ServiceCollection();
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:SigningKey"] = shortKey,
            ["JwtSettings:Issuer"] = "issuer",
            ["JwtSettings:Audience"] = "audience",
        });

        // Act
        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddGatewayAuthentication(config));

        // Assert
        Assert.Contains("JWT SigningKey must be at least 32 characters", ex.Message);
    }

    [Fact]
    public void AddGatewayAuthentication_WhenSigningKeyIsExactly32Chars_DoesNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var exactly32 = new string('x', 32);
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:SigningKey"] = exactly32,
            ["JwtSettings:Issuer"] = "issuer",
            ["JwtSettings:Audience"] = "audience",
        });

        // Act & Assert
        var ex = Record.Exception(() => services.AddGatewayAuthentication(config));
        Assert.Null(ex);
    }

    [Fact]
    public void AddGatewayAuthentication_WhenSigningKeyIsValid_RegistersJwtBearerAuthentication()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["JwtSettings:SigningKey"] = "a-valid-signing-key-at-least-32-chars!",
            ["JwtSettings:Issuer"] = "test-issuer",
            ["JwtSettings:Audience"] = "test-audience",
        });

        // Act
        services.AddGatewayAuthentication(config);
        var provider = services.BuildServiceProvider();

        // Assert — authentication services must be resolvable
        var authService = provider.GetService<Microsoft.AspNetCore.Authentication.IAuthenticationService>();
        Assert.NotNull(authService);
    }
}
