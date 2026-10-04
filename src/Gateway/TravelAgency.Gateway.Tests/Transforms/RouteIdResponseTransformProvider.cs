using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TravelAgency.Gateway.Tests.Transforms;

/// <summary>
/// Test-only transform so routing tests can see which YARP route was selected.
/// </summary>
public sealed class RouteIdResponseTransformProvider : ITransformProvider
{
    public const string HeaderName = "X-Yarp-Route-Id";

    public void Apply(TransformBuilderContext context)
    {
        context.AddResponseHeader(HeaderName, context.Route.RouteId, append: false);
    }

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }
}
