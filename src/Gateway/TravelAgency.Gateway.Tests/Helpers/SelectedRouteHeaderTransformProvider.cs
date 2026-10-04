using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace TravelAgency.Gateway.Tests.Helpers;

/// <summary>
/// Stamps the matched YARP route id onto the proxied request.
/// Routing tests read it back from the mock backend to assert which route was selected.
/// Registered only in the test host.
/// </summary>
internal sealed class SelectedRouteHeaderTransformProvider : ITransformProvider
{
    public const string HeaderName = "X-Yarp-Route-Id";

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        var routeId = context.Route.RouteId;
        context.AddRequestHeader(HeaderName, routeId, append: false);
    }
}
