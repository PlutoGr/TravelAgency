using Microsoft.Extensions.Configuration;
using Xunit;
using TravelAgency.Gateway.Configuration;

namespace TravelAgency.Gateway.Tests.Configuration;

public class CookieSettingsTests
{
    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Bind_WithDefaultSection_ReturnsDefaultValues()
    {
        // Arrange
        var config = BuildConfig(new Dictionary<string, string?>());
        var section = config.GetSection(CookieSettings.SectionName);

        // Act
        var settings = new CookieSettings();
        section.Bind(settings);

        // Assert - defaults from class when section is empty
        Assert.True(settings.Secure);
        Assert.Equal("/", settings.Path);
        Assert.Equal("Lax", settings.SameSite);
        Assert.Equal("access_token", settings.AccessTokenName);
        Assert.Equal("refresh_token", settings.RefreshTokenName);
    }

    [Fact]
    public void Bind_WithAllValues_BindsCorrectly()
    {
        // Arrange
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Cookie:Secure"] = "false",
            ["Cookie:Path"] = "/api/v1",
            ["Cookie:SameSite"] = "Strict",
            ["Cookie:Domain"] = ".travelagency.com",
            ["Cookie:AccessTokenName"] = "at",
            ["Cookie:RefreshTokenName"] = "rt",
        });
        var section = config.GetSection(CookieSettings.SectionName);

        // Act
        var settings = new CookieSettings();
        section.Bind(settings);

        // Assert
        Assert.False(settings.Secure);
        Assert.Equal("/api/v1", settings.Path);
        Assert.Equal("Strict", settings.SameSite);
        Assert.Equal(".travelagency.com", settings.Domain);
        Assert.Equal("at", settings.AccessTokenName);
        Assert.Equal("rt", settings.RefreshTokenName);
    }

    [Fact]
    public void Bind_WithEmptySection_DomainIsNull()
    {
        var config = BuildConfig(new Dictionary<string, string?>());
        var section = config.GetSection(CookieSettings.SectionName);
        var settings = new CookieSettings();
        section.Bind(settings);
        Assert.Null(settings.Domain);
    }

    [Fact]
    public void Bind_WithSameSiteNone_BindsCorrectly()
    {
        // Arrange
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Cookie:SameSite"] = "None",
        });
        var section = config.GetSection(CookieSettings.SectionName);

        // Act
        var settings = new CookieSettings();
        section.Bind(settings);

        // Assert
        Assert.Equal("None", settings.SameSite);
    }

    [Fact]
    public void SectionName_IsCookie()
    {
        Assert.Equal("Cookie", CookieSettings.SectionName);
    }
}
