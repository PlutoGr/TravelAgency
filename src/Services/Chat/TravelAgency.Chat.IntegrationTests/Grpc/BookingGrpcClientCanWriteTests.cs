using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TravelAgency.Chat.Infrastructure.GrpcClients;
using TravelAgency.Contracts.Grpc.Booking;
using TravelAgency.Shared.Infrastructure.GrpcServices;
using Xunit;

namespace TravelAgency.Chat.IntegrationTests.Grpc;

public class BookingGrpcClientCanWriteTests
{
    [Fact]
    public async Task CanWriteBookingAsync_WhenFieldIsAbsent_FallsBackToHasAccess()
    {
        using var allowed = await CreateTestServerAsync(hasAccess: true, canWrite: null);
        using var denied = await CreateTestServerAsync(hasAccess: false, canWrite: null);

        (await CreateClient(allowed).CanWriteBookingAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeTrue();
        (await CreateClient(denied).CanWriteBookingAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task CanWriteBookingAsync_WhenFieldIsPresent_UsesCanWrite()
    {
        using var explicitDeny = await CreateTestServerAsync(hasAccess: true, canWrite: false);
        using var explicitAllow = await CreateTestServerAsync(hasAccess: false, canWrite: true);

        (await CreateClient(explicitDeny).CanWriteBookingAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeFalse();
        (await CreateClient(explicitAllow).CanWriteBookingAsync(Guid.NewGuid(), Guid.NewGuid())).Should().BeTrue();
    }

    private static async Task<IHost> CreateTestServerAsync(bool hasAccess, bool? canWrite)
    {
        var plan = new CallPlan(hasAccess, canWrite);
        var host = Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddGrpc();
                    services.AddSingleton(plan);
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<MockBookingGrpcService>());
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static BookingGrpcClient CreateClient(IHost host)
    {
        var server = host.GetTestServer();
        var httpClient = server.CreateClient();
        var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = httpClient
        });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrpcSettings:InternalServiceToken"] = "test-token"
            })
            .Build();
        return new BookingGrpcClient(
            new BookingService.BookingServiceClient(channel),
            new GrpcAuthCallOptionsFactory(configuration),
            NullLogger<BookingGrpcClient>.Instance);
    }

    private sealed record CallPlan(bool HasAccess, bool? CanWrite);

    private sealed class MockBookingGrpcService : BookingService.BookingServiceBase
    {
        private readonly CallPlan _plan;

        public MockBookingGrpcService(CallPlan plan)
        {
            _plan = plan;
        }

        public override Task<ValidateBookingAccessResponse> ValidateBookingAccess(
            ValidateBookingAccessRequest request,
            ServerCallContext context)
        {
            var response = new ValidateBookingAccessResponse { HasAccess = _plan.HasAccess };
            if (_plan.CanWrite is bool canWrite)
                response.CanWrite = canWrite;
            return Task.FromResult(response);
        }
    }
}
