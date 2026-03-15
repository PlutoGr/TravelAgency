using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Chat.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddChatCors(this IServiceCollection services, IConfiguration configuration)
        => services.AddSharedCors(configuration, ["http://localhost:3000", "http://localhost:5173"]);

    public static IApplicationBuilder UseChatCors(this IApplicationBuilder app)
        => app.UseSharedCors();
}
