using TravelAgency.Gateway.Configuration;
using TravelAgency.Gateway.Transforms;

namespace TravelAgency.Gateway.Extensions;

public static class YarpExtensions
{
    public static IServiceCollection AddGatewayYarp(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CookieSettings>(configuration.GetSection(CookieSettings.SectionName));
        services.Configure<AuthRouteSettings>(configuration.GetSection(AuthRouteSettings.SectionName));
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms<AuthResponseTransformProvider>()
            .AddTransforms<AuthRequestTransformProvider>();

        return services;
    }

    public static WebApplication MapGatewayYarp(this WebApplication app)
    {
        // POST /media/presign отдаёт адрес хранилища. Снаружи его никто не вызывает
        // (фронт и другие сервисы ходят в файлы по id), поэтому Gateway маршрут не публикует.
        app.MapMethods(
            "/api/v1/media/presign",
            [HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete],
            () => Results.NotFound());

        app.MapReverseProxy();
        return app;
    }
}
