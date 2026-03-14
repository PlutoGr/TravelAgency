using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Chat.Application;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Chat.Domain.Interfaces;
using TravelAgency.Chat.Infrastructure.Persistence;
using TravelAgency.Chat.Infrastructure.Repositories;
using TravelAgency.Chat.Infrastructure.Services;

namespace TravelAgency.Chat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddChatInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ChatDb")
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'ChatDb' or 'DefaultConnection' must be configured.");

        services.AddDbContext<ChatDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IChatMessageRepository, MessageRepository>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddHttpClient<IBookingAccessService, BookingAccessService>();

        services.AddChatApplication();

        return services;
    }
}
