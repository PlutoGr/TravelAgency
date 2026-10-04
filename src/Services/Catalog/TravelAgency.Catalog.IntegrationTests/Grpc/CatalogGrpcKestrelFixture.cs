using System.Net;
using System.Net.Sockets;
using TravelAgency.Catalog.API.Middleware;
using TravelAgency.Shared.Infrastructure.Hosting;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Catalog.IntegrationTests.Grpc;

/// <summary>
/// Boots Catalog on real Kestrel. WebApplicationFactory's TestServer has no socket,
/// so it hides the cleartext HTTP/2 problem.
/// </summary>
public sealed class CatalogGrpcKestrelFixture : IAsyncLifetime
{
    public const string ServiceToken = "test-internal-token";

    private WebApplication? _app;

    public string Address { get; private set; } = string.Empty;

    public int GrpcPort { get; private set; }

    public string GrpcAddress => $"http://127.0.0.1:{GrpcPort}";

    public async Task InitializeAsync()
    {
        var restPort = FreeTcpPort();
        GrpcPort = FreeTcpPort(restPort);
        Address = $"http://127.0.0.1:{restPort}";

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(Program).Assembly.GetName().Name
        });

        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.WebHost.UseUrls(Address);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:CatalogDb"] = "Host=127.0.0.1;Port=1;Database=unused;Username=unused",
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
            ["JwtSettings:ValidateLifetime"] = "false",
            ["GrpcSettings:InternalServiceToken"] = ServiceToken,
            ["ServiceListen:GrpcPort"] = GrpcPort.ToString()
        });
        builder.UseServiceListenPorts();
        builder.Services.AddSingleton<IExceptionMapper, CatalogExceptionMapper>();
        Program.ConfigureServices(builder.Services, builder.Configuration);

        _app = builder.Build();
        Program.ConfigurePipeline(_app);
        await _app.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private static int FreeTcpPort(params int[] avoid)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        if (port is ServiceListenSettings.DefaultHttpPort or ServiceListenSettings.DefaultGrpcPort
            || avoid.Contains(port))
        {
            return FreeTcpPort(avoid);
        }

        return port;
    }
}
