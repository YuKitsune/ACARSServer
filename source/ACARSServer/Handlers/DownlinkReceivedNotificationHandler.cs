using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Handlers;

public class DownlinkReceivedNotificationHandler(
    IControllerManager controllerManager,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<DownlinkReceivedNotification>
{
    public async Task Handle(DownlinkReceivedNotification notification, CancellationToken cancellationToken)
    {
        var controllers = controllerManager.Controllers
            .Where(c =>
                c.FlightSimulationNetwork == notification.FlightSimulationNetwork &&
                c.StationIdentifier == notification.StationIdentifier)
            .ToArray();

        if (!controllers.Any())
        {
            logger.Information("No controllers found for downlink from {From}", notification.Downlink.Sender);
            return;
        }

        // Broadcast to all controllers connected to the station
        // TODO: Filter by jurisdiction.
        //  Plugin needs to ignore messages from flights not assumed.
        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("DownlinkReceived", notification.Downlink, cancellationToken);

        logger.Information("Relayed downlink from {From} to {StationIdentifier}", notification.Downlink.Sender, notification.StationIdentifier);
    }
}