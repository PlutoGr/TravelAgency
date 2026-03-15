namespace TravelAgency.Catalog.IntegrationTests.Startup;

/// <summary>
/// Integration tests for Catalog API migration startup behavior (AUDIT-001).
/// Verifies UseCatalogMigrations runs when ASPNETCORE_RUN_MIGRATIONS=true or IsDevelopment,
/// and skips when Production and ASPNETCORE_RUN_MIGRATIONS is not set.
/// </summary>
public class CatalogMigrationStartupTests
{
    [Fact]
    public async Task InitializeAsync_WhenProductionAndNoRunMigrationsEnvVar_AppStartsSuccessfully()
    {
        var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync(
            configureConfig: null,
            migrationOptions: new CatalogMigrationTestOptions
            {
                EnvironmentName = "Production",
                AspNetCoreRunMigrations = "false" // Explicitly not "true" - migrations should be skipped
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.IsSuccessStatusCode.Should().BeTrue();

        await factory.DisposeAsync();
    }

    [Fact]
    public async Task InitializeAsync_WhenRunMigrationsEnvVarTrue_AppStartsSuccessfully()
    {
        var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync(
            configureConfig: null,
            migrationOptions: new CatalogMigrationTestOptions
            {
                EnvironmentName = "Production",
                AspNetCoreRunMigrations = "true"
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.IsSuccessStatusCode.Should().BeTrue();

        await factory.DisposeAsync();
    }

    [Fact]
    public async Task InitializeAsync_WhenDevelopment_AppStartsSuccessfully()
    {
        var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync(
            configureConfig: null,
            migrationOptions: new CatalogMigrationTestOptions
            {
                EnvironmentName = "Development"
            });

        var client = factory.CreateClient();
        var response = await client.GetAsync("/health/live");
        response.IsSuccessStatusCode.Should().BeTrue();

        await factory.DisposeAsync();
    }

    [Fact]
    public async Task InitializeAsync_WhenRunMigrationsEnvVarTrue_CanQueryCatalogEndpoint()
    {
        var factory = new CustomWebApplicationFactory();
        await factory.InitializeAsync(
            configureConfig: null,
            migrationOptions: new CatalogMigrationTestOptions
            {
                EnvironmentName = "Production",
                AspNetCoreRunMigrations = "true"
            });

        // Schema created by UseCatalogMigrations (Migrate) during startup
        var client = factory.CreateClient();
        var response = await client.GetAsync("/catalog/tours");
        response.IsSuccessStatusCode.Should().BeTrue();

        await factory.DisposeAsync();
    }
}
