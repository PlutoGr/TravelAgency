using TravelAgency.Booking.API.Extensions;
using TravelAgency.Testing;

namespace TravelAgency.Booking.IntegrationTests.Grpc;

/// <summary>
/// Boots Booking on real Kestrel. WebApplicationFactory's TestServer has no socket,
/// so it hides the cleartext HTTP/2 problem.
/// </summary>
public sealed class BookingGrpcKestrelFixture : IAsyncLifetime
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
        builder.Host.AddBookingSerilog();
        _listeners.Configure(builder.WebHost);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:BookingDb"] = "Host=127.0.0.1;Port=1;Database=unused;Username=unused",
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
            ["JwtSettings:AccessTokenExpirationMinutes"] = "15",
            ["JwtSettings:ValidateLifetime"] = "false",
            ["GrpcClients:CatalogServiceUrl"] = "http://127.0.0.1:1",
            ["GrpcClients:IdentityServiceUrl"] = "http://127.0.0.1:1",
            ["GrpcSettings:InternalServiceToken"] = ServiceToken,
            ["ASPNETCORE_RUN_MIGRATIONS"] = "false"
        });
        Program.ConfigureServices(builder);

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
}
