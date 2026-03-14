// Microsoft.OpenApi 2.x: types are in Microsoft.OpenApi (not .Models)
using Microsoft.OpenApi;

namespace TravelAgency.Catalog.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCatalogSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "TravelAgency.Catalog API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "JWT Bearer token",
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
        });
        return services;
    }
}
