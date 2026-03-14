using ChatInfraExt = TravelAgency.Chat.Infrastructure.Extensions.InfrastructureExtensions;

namespace TravelAgency.Chat.API.Extensions;

public static class InfrastructureExtensions
{
    public static IApplicationBuilder UseChatMigrations(this IApplicationBuilder app)
        => ChatInfraExt.UseChatMigrations(app);
}
