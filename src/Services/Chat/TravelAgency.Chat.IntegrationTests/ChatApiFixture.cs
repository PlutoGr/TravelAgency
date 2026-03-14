using Xunit;

namespace TravelAgency.Chat.IntegrationTests;

/// <summary>
/// xUnit collection fixture for Chat API integration tests. Shares WebApplicationFactory and PostgreSQL container.
/// </summary>
[CollectionDefinition("Chat API")]
public sealed class ChatApiCollection : ICollectionFixture<ChatApiFixture>;

public sealed class ChatApiFixture : IAsyncLifetime
{
    private readonly ChatApiApplicationFactory _factory = new();

    /// <summary>
    /// Exposes the factory for tests that need to configure mocks (e.g. IBookingAccessService).
    /// </summary>
    public ChatApiApplicationFactory Factory => _factory;

    public HttpClient CreateClient() => _factory.CreateClient();

    public Task InitializeAsync() => _factory.InitializeAsync();

    public Task DisposeAsync() => _factory.DisposeAsync();
}
