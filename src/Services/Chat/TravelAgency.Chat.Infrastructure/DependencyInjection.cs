using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Chat.Application;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Chat.Domain.Interfaces;
using TravelAgency.Chat.Infrastructure.GrpcClients;
using TravelAgency.Chat.Infrastructure.Persistence;
using TravelAgency.Chat.Infrastructure.Repositories;
using TravelAgency.Contracts.Grpc.Booking;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Chat.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddChatInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ChatDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:ChatDb is required. Set ConnectionStrings__ChatDb environment variable or add it to configuration.");

        services.AddDbContext<ChatDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IChatMessageRepository, MessageRepository>();

        services.AddHttpContextAccessor();
        services.AddCurrentUserService();
        services.AddGrpcAuthCallOptionsFactory();

        var bookingGrpcAddress = configuration["GrpcClients:BookingServiceUrl"]
            ?? configuration["Services:BookingServiceUrl"]
            ?? "http://localhost:5030";
        services.AddGrpcClient<BookingService.BookingServiceClient>(o => o.Address = new Uri(bookingGrpcAddress));
        services.AddScoped<IBookingGrpcClient, BookingGrpcClient>();

        services.AddChatApplication();
        services.AddSharedMediatRBehaviors();

        return services;
    }
}
