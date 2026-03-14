using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.PostgreSql;
using TravelAgency.Chat.Application.Abstractions;
using Xunit;

namespace TravelAgency.Chat.IntegrationTests;

/// <summary>
/// WebApplicationFactory for Chat API integration tests. Uses TestContainers PostgreSQL.
/// Mocks IBookingAccessService to avoid real Booking service calls.
/// </summary>
public sealed class ChatApiApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("TravelAgency_Chat_Test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>
    /// Mock for IBookingAccessService. Default: returns true for any bookingId.
    /// Configure per-test for GetMessages_WithInvalidBooking_Returns403.
    /// </summary>
    public Mock<IBookingAccessService> BookingAccessServiceMock { get; } = new();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Override connection string so AddChatInfrastructure uses TestContainers PostgreSQL
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("ConnectionStrings:Redis", ""); // Disable Redis for tests

        // Mock IBookingAccessService so we don't need a real Booking service.
        // Default: return true for any bookingId.
        builder.ConfigureServices(services =>
        {
            BookingAccessServiceMock
                .Setup(x => x.CanAccessBookingAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            services.RemoveAll<IBookingAccessService>();
            services.AddSingleton<IBookingAccessService>(_ => BookingAccessServiceMock.Object);
        });
    }
}
