// Microsoft.OpenApi 2.x: types are in Microsoft.OpenApi (not .Models)
using Microsoft.OpenApi;

namespace TravelAgency.Media.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddMediaSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "TravelAgency Media API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token"
            });
        });
        return services;
    }

    public static IApplicationBuilder UseMediaSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "TravelAgency Media API v1");
        });
        return app;
    }
}
