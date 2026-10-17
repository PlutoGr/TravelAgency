using Microsoft.Extensions.DependencyInjection.Extensions;
using TravelAgency.Identity.API.Extensions;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Testing;

namespace TravelAgency.Identity.IntegrationTests.Grpc;

/// <summary>
/// Boots Identity on real Kestrel. WebApplicationFactory's TestServer has no socket,
/// so it hides the cleartext HTTP/2 problem.
/// </summary>
public sealed class IdentityGrpcKestrelFixture : IAsyncLifetime
{
    public const string ServiceToken = "test-internal-token";

    private readonly KestrelLoopbackListeners _listeners = new();

    private WebApplication? _app;

    public string Address { get; private set; } = string.Empty;

    public int GrpcPort { get; private set; }

    public string GrpcAddress { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(Program).Assembly.GetName().Name
        });

        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Host.AddIdentitySerilog();
        _listeners.Configure(builder.WebHost);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:IdentityDb"] = "Host=127.0.0.1;Port=1;Database=unused;Username=unused",
            ["ConnectionStrings:Redis"] = string.Empty,
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
            ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
            ["JwtSettings:RefreshTokenExpirationDays"] = "7",
            ["JwtSettings:ValidateLifetime"] = "false",
            ["GrpcSettings:InternalServiceToken"] = ServiceToken,
            ["RateLimit:PermitLimit"] = "1000",
            ["ASPNETCORE_RUN_MIGRATIONS"] = "false",
            ["ASPNETCORE_SEED_DATA"] = "false"
        });
        Program.ConfigureServices(builder);
        // GetUserSummary has no OK path without a user row, and this fixture has no database.
        // A fixed user lets the REST-port test probe the gRPC port before asserting the refusal.
        builder.Services.RemoveAll<IUserRepository>();
        builder.Services.AddSingleton<IUserRepository>(new FixedGrpcUserRepository());

        _app = builder.Build();
        Program.ConfigurePipeline(_app);
        await _app.StartAsync();

        Address = _listeners.RestAddress;
        GrpcPort = _listeners.GrpcPort;
        GrpcAddress = _listeners.GrpcAddress;
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        _listeners.Dispose();
    }

    private sealed class FixedGrpcUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult<User?>(User.Create("grpc-live@test.com", "hash", "Live", "Probe", null));

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult<User?>(null);

        public Task AddAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

        public void Stage(User user)
        {
        }

        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(false);
    }
}
