namespace TravelAgency.Media.API.Hosting;

/// <summary>
/// gRPC listens on its own cleartext HTTP/2 port. REST stays on the public HTTP port
/// (8080 in Docker). 8081 is reachable only inside the docker network.
/// </summary>
internal static class MediaPorts
{
    public const int Grpc = 8081;
    public const string GrpcHostPattern = "*:8081";
}
