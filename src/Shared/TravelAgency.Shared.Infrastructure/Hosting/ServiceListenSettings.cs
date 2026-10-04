using Microsoft.Extensions.Configuration;

namespace TravelAgency.Shared.Infrastructure.Hosting;

/// <summary>
/// Ports for the public HTTP listener and the cleartext HTTP/2 gRPC listener.
/// Defaults match the container layout: REST on 8080, gRPC on 8081.
/// Override with configuration <c>ServiceListen:HttpPort</c> and <c>ServiceListen:GrpcPort</c>
/// (environment <c>ServiceListen__HttpPort</c> / <c>ServiceListen__GrpcPort</c>).
/// </summary>
public sealed class ServiceListenSettings
{
    public const string SectionName = "ServiceListen";

    public const int DefaultHttpPort = 8080;

    public const int DefaultGrpcPort = 8081;

    public int HttpPort { get; set; } = DefaultHttpPort;

    public int GrpcPort { get; set; } = DefaultGrpcPort;

    public string GrpcHostPattern => $"*:{GrpcPort}";

    public static ServiceListenSettings FromConfiguration(IConfiguration configuration)
    {
        var settings = new ServiceListenSettings();
        configuration.GetSection(SectionName).Bind(settings);
        if (settings.HttpPort <= 0)
            settings.HttpPort = DefaultHttpPort;
        if (settings.GrpcPort <= 0)
            settings.GrpcPort = DefaultGrpcPort;
        return settings;
    }
}
