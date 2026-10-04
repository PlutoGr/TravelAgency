using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TravelAgency.Shared.Infrastructure.Hosting;

namespace TravelAgency.Shared.Infrastructure.Tests.Hosting;

public class ServiceListenTests
{
    [Fact]
    public void Defaults_AreRest8080_AndGrpc8081()
    {
        var settings = ServiceListenSettings.FromConfiguration(new ConfigurationBuilder().Build());

        settings.HttpPort.Should().Be(8080);
        settings.GrpcPort.Should().Be(8081);
        settings.GrpcHostPattern.Should().Be("*:8081");
    }

    [Fact]
    public void Configuration_OverridesPorts_AndZeroFallsBackToDefaults()
    {
        var overridden = ServiceListenSettings.FromConfiguration(Config(
            ("ServiceListen:HttpPort", "9090"),
            ("ServiceListen:GrpcPort", "9091")));
        overridden.HttpPort.Should().Be(9090);
        overridden.GrpcPort.Should().Be(9091);

        var zeroed = ServiceListenSettings.FromConfiguration(Config(("ServiceListen:GrpcPort", "0")));
        zeroed.HttpPort.Should().Be(8080);
        zeroed.GrpcPort.Should().Be(8081);
    }

    [Fact]
    public void PublicUrls_PreferUrlsOverPorts()
    {
        var configuration = Config(
            ("urls", "http://127.0.0.1:5277"),
            ("http_ports", "8080"));

        PublicEndpointUrls.Resolve(configuration).Should().Equal("http://127.0.0.1:5277");
    }

    [Fact]
    public void PublicUrls_ExpandHttpAndHttpsPorts()
    {
        var configuration = Config(
            ("http_ports", "8080"),
            ("https_ports", "8443"));

        PublicEndpointUrls.Resolve(configuration).Should().Equal("http://*:8080", "https://*:8443");
    }

    [Fact]
    public async Task Middleware_RefusesRestOnGrpcPort_AndGrpcOnRestPort()
    {
        var options = Options.Create(new ServiceListenSettings());
        var middleware = new GrpcListenPortMiddleware(_ => Task.CompletedTask, options);

        var restOnGrpc = new DefaultHttpContext();
        restOnGrpc.Connection.LocalPort = ServiceListenSettings.DefaultGrpcPort;
        await middleware.InvokeAsync(restOnGrpc);
        restOnGrpc.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);

        var grpcOnRest = new DefaultHttpContext();
        grpcOnRest.Connection.LocalPort = ServiceListenSettings.DefaultHttpPort;
        grpcOnRest.Request.ContentType = "application/grpc";
        await middleware.InvokeAsync(grpcOnRest);
        grpcOnRest.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Middleware_AllowsMatchingTraffic_AndSkipsInMemoryServer()
    {
        var reached = 0;
        var options = Options.Create(new ServiceListenSettings());
        var middleware = new GrpcListenPortMiddleware(_ =>
        {
            reached++;
            return Task.CompletedTask;
        }, options);

        var grpc = new DefaultHttpContext();
        grpc.Connection.LocalPort = ServiceListenSettings.DefaultGrpcPort;
        grpc.Request.ContentType = "application/grpc+proto";
        await middleware.InvokeAsync(grpc);
        grpc.Response.StatusCode.Should().Be(StatusCodes.Status200OK);

        var rest = new DefaultHttpContext();
        rest.Connection.LocalPort = ServiceListenSettings.DefaultHttpPort;
        await middleware.InvokeAsync(rest);
        rest.Response.StatusCode.Should().Be(StatusCodes.Status200OK);

        var inMemory = new DefaultHttpContext();
        inMemory.Request.ContentType = "application/grpc";
        await middleware.InvokeAsync(inMemory);

        reached.Should().Be(3);
    }

    [Fact]
    public async Task PublicUrl_OnGrpcPort_IsRejected()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.Logging.SetMinimumLevel(LogLevel.None);
        builder.WebHost.UseUrls($"http://127.0.0.1:{ServiceListenSettings.DefaultGrpcPort}");
        builder.UseServiceListenPorts();

        var act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*reserved for gRPC*");
    }

    private static IConfiguration Config(params (string Key, string? Value)[] values)
    {
        var data = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }
}
