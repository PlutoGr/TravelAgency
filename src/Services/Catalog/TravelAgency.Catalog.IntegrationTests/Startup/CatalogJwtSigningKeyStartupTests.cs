namespace TravelAgency.Catalog.IntegrationTests.Startup;

/// <summary>
/// Integration tests for Catalog API JWT SigningKey startup validation (T1).
/// Verifies that the API fails to start when SigningKey is missing/empty and succeeds when provided.
/// </summary>
public class CatalogJwtSigningKeyStartupTests
{
    private const string ValidSigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC";

    [Fact]
    public async Task InitializeAsync_WhenSigningKeyIsMissing_ThrowsInvalidOperationException()
    {
        var factory = new CustomWebApplicationFactory();
        var act = () => factory.InitializeAsync(d => d.Remove("JwtSettings:SigningKey"));

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("SigningKey");
        ex.Which.Message.Should().Contain("JWT_SIGNING_KEY");
    }

    [Fact]
    public async Task InitializeAsync_WhenSigningKeyIsEmpty_ThrowsInvalidOperationException()
    {
        var factory = new CustomWebApplicationFactory();
        var act = () => factory.InitializeAsync(d => d["JwtSettings:SigningKey"] = "");

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("SigningKey");
    }

    [Fact]
    public async Task InitializeAsync_WhenSigningKeyIsWhitespace_ThrowsInvalidOperationException()
    {
        var factory = new CustomWebApplicationFactory();
        var act = () => factory.InitializeAsync(d => d["JwtSettings:SigningKey"] = "   ");

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("SigningKey");
    }

    [Fact]
    public async Task InitializeAsync_WhenSigningKeyIsTooShort_ThrowsInvalidOperationException()
    {
        var factory = new CustomWebApplicationFactory();
        var act = () => factory.InitializeAsync(d => d["JwtSettings:SigningKey"] = "short");

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("32 characters");
    }

    [Fact]
    public async Task InitializeAsync_WhenSigningKeyProvidedViaConfig_StartsSuccessfully()
    {
        var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync();

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.IsSuccessStatusCode.Should().BeTrue();

        await factory.DisposeAsync();
    }

    [Fact]
    public async Task InitializeAsync_WhenSigningKeyProvidedViaEnvVar_StartsSuccessfully()
    {
        var previous = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY");
        try
        {
            Environment.SetEnvironmentVariable("JWT_SIGNING_KEY", ValidSigningKey);

            var factory = new CustomWebApplicationFactory();
            await factory.InitializeAsync(d => d.Remove("JwtSettings:SigningKey"));

            var client = factory.CreateClient();
            var response = await client.GetAsync("/health/live");
            response.IsSuccessStatusCode.Should().BeTrue();

            await factory.DisposeAsync();
        }
        finally
        {
            Environment.SetEnvironmentVariable("JWT_SIGNING_KEY", previous);
        }
    }
}
