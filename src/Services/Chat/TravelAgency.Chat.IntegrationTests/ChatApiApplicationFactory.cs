using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Testcontainers.PostgreSql;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Chat.Infrastructure.Persistence;
using TravelAgency.Shared.Contracts.Authorization;
using Xunit;

namespace TravelAgency.Chat.IntegrationTests;

/// <summary>
/// WebApplicationFactory for Chat API integration tests. Uses TestContainers PostgreSQL.
/// Mocks IBookingGrpcClient to avoid real Booking gRPC calls.
/// Mocks ICurrentUserService so SignalR Hub context has valid UserId (HttpContext.User not populated in test server).
/// </summary>
public sealed class ChatApiApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Test user ID used when ICurrentUserService is mocked. Matches JwtTokenHelper when used with same Guid.
    /// </summary>
    public static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    static ChatApiApplicationFactory()
    {
        // AddChatAuthentication reads JwtSettings__SigningKey eagerly during host build.
        Environment.SetEnvironmentVariable("JwtSettings__SigningKey", "TestSigningKeyWithAtLeast32CharactersForHMAC");
    }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("TravelAgency_Chat_Test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    /// <summary>
    /// Mock for IBookingGrpcClient. Default: returns true for any bookingId/userId.
    /// Configure per-test for GetMessages_WithInvalidBooking_Returns403.
    /// </summary>
    public Mock<IBookingGrpcClient> BookingGrpcClientMock { get; } = new();

    /// <summary>
    /// Mock for ICurrentUserService. Returns valid UserId for SignalR tests where HttpContext.User is empty.
    /// </summary>
    public Mock<ICurrentUserService> CurrentUserServiceMock { get; } = new();

    public async Task InitializeAsync() => await _postgres.StartAsync();

    public new async Task DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_RUN_MIGRATIONS", null);
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Force migrations to run in tests (UseChatMigrations checks env var).
        Environment.SetEnvironmentVariable("ASPNETCORE_RUN_MIGRATIONS", "true");

        // ASPNETCORE_RUN_MIGRATIONS=true forces migrations to run in tests (UseChatMigrations checks config).
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Redis"] = string.Empty, // Disable Redis for tests
                ["ASPNETCORE_RUN_MIGRATIONS"] = "true",
                ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
            });
        });

        // ConfigureTestServices runs AFTER AddChatInfrastructure. Replace ChatDbContext with one that uses
        // Testcontainers connection string directly (config override doesn't work - DbContext is registered
        // before our override is applied). MessageRepository resolves ChatDbContext from DI, so no re-registration needed.
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ChatDbContext>>();
            services.RemoveAll<ChatDbContext>();

            var options = new DbContextOptionsBuilder<ChatDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            services.AddScoped<ChatDbContext>(sp => new ChatDbContext(options));
            services.AddScoped<DbContextOptions<ChatDbContext>>(_ => options);

            BookingGrpcClientMock
                .Setup(x => x.ValidateBookingAccessAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            services.RemoveAll<IBookingGrpcClient>();
            services.AddSingleton<IBookingGrpcClient>(_ => BookingGrpcClientMock.Object);

            // Mock ICurrentUserService: SignalR Hub in WebApplicationFactory doesn't populate HttpContext.User.
            // SendMessageCommandHandler requires UserId != Guid.Empty.
            CurrentUserServiceMock
                .Setup(x => x.UserId)
                .Returns(TestUserId);
            CurrentUserServiceMock
                .Setup(x => x.Role)
                .Returns(AppRoles.Client);
            CurrentUserServiceMock
                .Setup(x => x.IsAuthenticated)
                .Returns(true);
            CurrentUserServiceMock
                .Setup(x => x.Email)
                .Returns(string.Empty);
            CurrentUserServiceMock
                .Setup(x => x.DisplayName)
                .Returns("Test User");

            services.RemoveAll<ICurrentUserService>();
            services.AddSingleton<ICurrentUserService>(_ => CurrentUserServiceMock.Object);
        });
    }
}
