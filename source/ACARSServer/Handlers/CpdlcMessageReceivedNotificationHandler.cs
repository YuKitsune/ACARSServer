using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Handlers;

public class CpdlcMessageReceivedNotificationHandler(
    IControllerManager controllerManager,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<CpdlcDownlinkMessageReceivedNotification>
{
    public async Task Handle(CpdlcDownlinkMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var controllers = controllerManager.Controllers
            .Where(c =>
                c.FlightSimulationNetwork == notification.FlightSimulationNetwork &&
                c.StationIdentifier == notification.StationIdentifier)
            .ToArray();

        if (!controllers.Any())
        {
            logger.Information("No controllers found for message from {From}", notification.Downlink.Sender);
            return;
        }

        // Broadcast to all controllers connected to the station
        // TODO: Filter by jurisdiction.
        //  Plugin needs to ignore messages from flights not assumed.
        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("ReceiveCpdlcMessage", notification.Downlink, cancellationToken);

        logger.Information("Relayed CPDLC message from {From} to {StationIdentifier}", notification.Downlink.Sender, notification.StationIdentifier);
    }
}