// Microsoft.OpenApi 2.x: types are in Microsoft.OpenApi (not .Models)
using Microsoft.OpenApi;

namespace TravelAgency.Booking.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddBookingSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "TravelAgency Booking API",
                Version = "v1"
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

        });

        return services;
    }

    public static WebApplication UseBookingSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "TravelAgency Booking API v1");
        });

        return app;
    }
}
