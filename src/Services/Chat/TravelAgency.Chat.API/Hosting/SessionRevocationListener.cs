using TravelAgency.Chat.API.Hubs;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Chat.API.Hosting;

public sealed class SessionRevocationListener(
    ISessionRevocationListener listener,
    ChatConnectionRegistry registry) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        listener.ListenAsync(registry.Abort, stoppingToken);
}
