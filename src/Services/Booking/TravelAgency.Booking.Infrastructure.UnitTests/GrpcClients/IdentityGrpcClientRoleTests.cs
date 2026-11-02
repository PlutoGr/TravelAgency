using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TravelAgency.Booking.Application.Abstractions;
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

    [Fact]
    public async Task ReadCardProfileAsync_ReturnsAvatarAndDisplayName_AndSplitsNotFoundFromOtherFailures()
    {
        using var foundHost = await CreateTestServerAsync(new CallPlan(
            Role: AppRoles.Client,
            DisplayName: "  Анна Соколова  ",
            AvatarUrl: "/api/v1/media/files/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/a256"));
        var found = await CreateClient(foundHost).ReadCardProfileAsync(Guid.NewGuid(), CancellationToken.None);
        found.Kind.Should().Be(IdentityReadKind.Found);
        found.Profile!.Email.Should().Be("manager@example.com");
        found.Profile.DisplayName.Should().Be("  Анна Соколова  ");
        found.Profile.Role.Should().Be(AppRoles.Client);
        found.Profile.AvatarUrl.Should().Be("/api/v1/media/files/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/a256");

        using var missingHost = await CreateTestServerAsync(new CallPlan(Error: StatusCode.NotFound));
        var missing = await CreateClient(missingHost).ReadCardProfileAsync(Guid.NewGuid(), CancellationToken.None);
        missing.Kind.Should().Be(IdentityReadKind.NotFound);
        missing.Profile.Should().BeNull();

        using var downHost = await CreateTestServerAsync(new CallPlan(Error: StatusCode.Unavailable));
        var down = await CreateClient(downHost).ReadCardProfileAsync(Guid.NewGuid(), CancellationToken.None);
        down.Kind.Should().Be(IdentityReadKind.Unavailable);
        down.Profile.Should().BeNull();
    }

    [Fact]
    public async Task ReadUserPhoneAsync_ReturnsStoredOrEmptyPhone_AndDoesNotTurnFailureIntoEmptyPhone()
    {
        using var foundHost = await CreateTestServerAsync(new CallPlan(Phone: "+79991234567"));
        var found = await CreateClient(foundHost).ReadUserPhoneAsync(Guid.NewGuid(), CancellationToken.None);
        found.Kind.Should().Be(IdentityReadKind.Found);
        found.Phone.Should().Be("+79991234567");

        using var emptyHost = await CreateTestServerAsync(new CallPlan(Phone: ""));
        var empty = await CreateClient(emptyHost).ReadUserPhoneAsync(Guid.NewGuid(), CancellationToken.None);
        empty.Kind.Should().Be(IdentityReadKind.Found);
        empty.Phone.Should().BeEmpty();

        using var missingHost = await CreateTestServerAsync(new CallPlan(Error: StatusCode.NotFound));
        var missing = await CreateClient(missingHost).ReadUserPhoneAsync(Guid.NewGuid(), CancellationToken.None);
        missing.Kind.Should().Be(IdentityReadKind.NotFound);

        using var downHost = await CreateTestServerAsync(new CallPlan(Error: StatusCode.Unavailable));
        var down = await CreateClient(downHost).ReadUserPhoneAsync(Guid.NewGuid(), CancellationToken.None);
        down.Kind.Should().Be(IdentityReadKind.Unavailable);
        down.Phone.Should().BeEmpty();
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

    private sealed record CallPlan(
        StatusCode? Error = null,
        string Role = AppRoles.Manager,
        string Phone = "+79991234567",
        string DisplayName = "Test Manager",
        string AvatarUrl = "");

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
                Role = _plan.Role,
                DisplayName = _plan.DisplayName,
                AvatarUrl = _plan.AvatarUrl
            });
        }

        public override Task<UserPhoneResponse> GetUserPhone(
            GetUserPhoneRequest request,
            ServerCallContext context)
        {
            if (_plan.Error is StatusCode statusCode)
                throw new RpcException(new Status(statusCode, "identity"));

            return Task.FromResult(new UserPhoneResponse
            {
                UserId = request.UserId,
                Phone = _plan.Phone
            });
        }
    }
}
