using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TravelAgency.Booking.Infrastructure.GrpcClients;
using TravelAgency.Contracts.Grpc.Identity;
using TravelAgency.Shared.Contracts.Authorization;
using TravelAgency.Shared.Infrastructure.GrpcServices;

namespace TravelAgency.Booking.Infrastructure.UnitTests.GrpcClients;

public class IdentityGrpcClientRoleTests
{
    [Fact]
    public async Task GetUserRoleAsync_WhenUserExists_ReturnsRole()
    {
        using var host = await CreateTestServerAsync(new CallPlan(Role: AppRoles.Manager));
        var client = CreateClient(host);

        var result = await client.GetUserRoleAsync(Guid.NewGuid(), CancellationToken.None);

        result.UserFound.Should().BeTrue();
        result.Role.Should().Be(AppRoles.Manager);
    }

    [Fact]
    public async Task GetUserRoleAsync_WhenNotFound_ReturnsUserMissing()
    {
        using var host = await CreateTestServerAsync(new CallPlan(Error: StatusCode.NotFound));
        var client = CreateClient(host);

        var result = await client.GetUserRoleAsync(Guid.NewGuid(), CancellationToken.None);

        result.UserFound.Should().BeFalse();
        result.Role.Should().BeEmpty();
    }

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    [InlineData(StatusCode.Internal)]
    public async Task GetUserRoleAsync_WhenTransportFails_PropagatesStatus(StatusCode statusCode)
    {
        using var host = await CreateTestServerAsync(new CallPlan(Error: statusCode));
        var client = CreateClient(host);

        var act = async () => await client.GetUserRoleAsync(Guid.NewGuid(), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(statusCode);
    }

    [Fact]
    public async Task GetUserSummaryAsync_WhenTransportFails_StillReturnsNull()
    {
        using var host = await CreateTestServerAsync(new CallPlan(Error: StatusCode.Unavailable));
        var client = CreateClient(host);

        var result = await client.GetUserSummaryAsync(Guid.NewGuid(), CancellationToken.None);

        result.Should().BeNull();
    }

    private static async Task<IHost> CreateTestServerAsync(CallPlan plan)
    {
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
                    app.UseEndpoints(endpoints => endpoints.MapGrpcService<MockIdentityGrpcService>());
                });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static IdentityGrpcClient CreateClient(IHost host)
    {
        var server = host.GetTestServer();
        var httpClient = server.CreateClient();
        var channel = GrpcChannel.ForAddress(httpClient.BaseAddress!, new GrpcChannelOptions
        {
            HttpClient = httpClient
        });
        var grpcClient = new IdentityGrpc.IdentityGrpcClient(channel);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrpcSettings:InternalServiceToken"] = "test-token"
            })
            .Build();
        return new IdentityGrpcClient(
            grpcClient,
            new GrpcAuthCallOptionsFactory(configuration),
            NullLogger<IdentityGrpcClient>.Instance);
    }

    private sealed record CallPlan(StatusCode? Error = null, string Role = AppRoles.Manager);

    private sealed class MockIdentityGrpcService : IdentityGrpc.IdentityGrpcBase
    {
        private readonly CallPlan _plan;

        public MockIdentityGrpcService(CallPlan plan)
        {
            _plan = plan;
        }

        public override Task<UserSummaryResponse> GetUserSummary(
            GetUserSummaryRequest request,
            ServerCallContext context)
        {
            if (_plan.Error is StatusCode statusCode)
                throw new RpcException(new Status(statusCode, "identity"));

            return Task.FromResult(new UserSummaryResponse
            {
                UserId = request.UserId,
                Email = "manager@example.com",
                FirstName = "Test",
                LastName = "Manager",
                Role = _plan.Role
            });
        }
    }
}
