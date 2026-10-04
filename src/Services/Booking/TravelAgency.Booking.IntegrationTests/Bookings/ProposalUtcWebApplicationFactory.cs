using System.Data;
using System.Text;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Infrastructure.Persistence;
using TravelAgency.Contracts.Grpc.Catalog;

namespace TravelAgency.Booking.IntegrationTests.Bookings;

/// <summary>
/// Booking API on real Postgres. Catalog is a gRPC stub reached through the real
/// <c>CatalogGrpcClient</c>; only the transport client is replaced.
/// </summary>
public sealed class ProposalUtcWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    /// <summary>
    /// Same instant Catalog would send: <c>DateTimeKind.Utc</c> formatted with <c>"O"</c>, which ends in Z.
    /// </summary>
    public static readonly DateTime CatalogSnapshotTakenAtUtc = new(2026, 4, 4, 10, 15, 30, DateTimeKind.Utc);

    public static string CatalogSnapshotTakenAtWire => CatalogSnapshotTakenAtUtc.ToString("O");

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("travel_booking_proposal_utc")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private IHost? _catalogHost;
    private HttpClient? _catalogHttpClient;
    private GrpcChannel? _catalogChannel;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _catalogHost = await StartCatalogStubAsync();
        var server = _catalogHost.GetTestServer();
        _catalogHttpClient = server.CreateClient();
        var baseAddress = _catalogHttpClient.BaseAddress ?? new Uri("http://localhost");
        _catalogChannel = GrpcChannel.ForAddress(baseAddress, new GrpcChannelOptions
        {
            HttpClient = _catalogHttpClient,
            DisposeHttpClient = false
        });

        using var client = CreateClient();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        _catalogChannel?.Dispose();
        _catalogHttpClient?.Dispose();
        if (_catalogHost is not null)
        {
            await _catalogHost.StopAsync();
            _catalogHost.Dispose();
        }

        await _postgres.DisposeAsync();
    }

    public async Task<DateTime> ReadSnapshotTakenAtAsync(Guid bookingId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            """SELECT "TourSnapshot_SnapshotTakenAt" FROM "Proposals" WHERE "BookingId" = @bookingId""";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "bookingId";
        parameter.Value = bookingId;
        command.Parameters.Add(parameter);

        var value = await command.ExecuteScalarAsync();
        return (DateTime)value!;
    }

    public async Task<IReadOnlyList<int>> ReadStatusHistoryAsync(Guid bookingId)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT \"Status\" FROM \"BookingStatusHistories\" WHERE \"BookingId\" = @bookingId ORDER BY \"ChangedAt\"";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "bookingId";
        parameter.Value = bookingId;
        command.Parameters.Add(parameter);

        var statuses = new List<int>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            statuses.Add(reader.GetInt32(0));

        return statuses;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:BookingDb", "Host=localhost;Database=travel_booking_placeholder");
        builder.UseSetting("JwtSettings:SigningKey", "TestSigningKeyWithAtLeast32CharactersForHMAC");
        builder.UseSetting("ASPNETCORE_RUN_MIGRATIONS", "false");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "15",
                ["JwtSettings:ValidateLifetime"] = "true",
                ["ConnectionStrings:BookingDb"] = "Host=localhost;Database=travel_booking_placeholder",
                ["GrpcClients:CatalogServiceUrl"] = "http://127.0.0.1:1",
                ["GrpcClients:IdentityServiceUrl"] = "http://127.0.0.1:1",
                ["GrpcSettings:InternalServiceToken"] = "test-internal-token",
                ["ASPNETCORE_RUN_MIGRATIONS"] = "false",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<BookingDbContext>>();
            services.RemoveAll<BookingDbContext>();

            var options = new DbContextOptionsBuilder<BookingDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

            services.AddScoped<BookingDbContext>(_ => new BookingDbContext(options));
            services.AddScoped<DbContextOptions<BookingDbContext>>(_ => options);

            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BookingDbContext>());

            // Keep CatalogGrpcClient. Replace only the generated transport so the string parse still runs.
            services.RemoveAll<CatalogService.CatalogServiceClient>();
            services.AddSingleton(_ => new CatalogService.CatalogServiceClient(_catalogChannel!));

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, jwt =>
            {
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "TestIssuer",
                    ValidAudience = "TestAudience",
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("TestSigningKeyWithAtLeast32CharactersForHMAC")),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });
        });
    }

    private static async Task<IHost> StartCatalogStubAsync()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services => services.AddGrpc());
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<CatalogSnapshotStub>());
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private sealed class CatalogSnapshotStub : CatalogService.CatalogServiceBase
    {
        public override Task<TourSnapshotResponse> GetTourSnapshot(
            GetTourSnapshotRequest request,
            ServerCallContext context)
        {
            return Task.FromResult(new TourSnapshotResponse
            {
                TourId = request.TourId,
                Title = "Мальдивы",
                Description = "Снимок каталога",
                Price = 289000,
                Currency = "RUB",
                DurationDays = 7,
                SnapshotTakenAt = CatalogSnapshotTakenAtWire,
                Found = true
            });
        }

        public override Task<TourSnapshotResponse> GetTourSnapshotForExistingBooking(
            GetTourSnapshotRequest request,
            ServerCallContext context) =>
            GetTourSnapshot(request, context);
    }
}
