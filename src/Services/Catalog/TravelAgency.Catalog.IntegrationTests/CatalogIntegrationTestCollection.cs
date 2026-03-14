namespace TravelAgency.Catalog.IntegrationTests;

[CollectionDefinition(nameof(CatalogIntegrationTestCollection))]
public class CatalogIntegrationTestCollection : ICollectionFixture<CatalogTestFixture>
{
}

/// <summary>
/// Shared async fixture that initializes and tears down the test server once
/// for all tests in the collection.
/// </summary>
public class CatalogTestFixture : IAsyncLifetime
{
    public CustomWebApplicationFactory Factory { get; } = new();

    public async Task InitializeAsync()
    {
        await Factory.InitializeAsync();
        Factory.EnsureDbCreated();
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
    }
}
