using ACARSServer.Contracts;
using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Handlers;

public class AircraftConnectedNotificationHandler(
    IControllerManager controllerManager,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<AircraftConnected>
{
    public async Task Handle(AircraftConnected notification, CancellationToken cancellationToken)
    {
        logger.Information(
            "Aircraft {Callsign} connected on {Network}/{StationId} with data authority state {DataAuthorityState}",
            notification.Callsign,
            notification.FlightSimulationNetwork,
            notification.StationId,
            notification.DataAuthorityState);

        // Find all controllers on the same network and station
        var controllers = controllerManager.Controllers
            .Where(c =>
                c.FlightSimulationNetwork == notification.FlightSimulationNetwork &&
                c.StationIdentifier == notification.StationId)
            .ToArray();

        if (!controllers.Any())
        {
            logger.Information(
                "No controllers to notify about connected aircraft {Callsign}",
                notification.Callsign);
            return;
        }

        // Notify all controllers that an aircraft has connected
        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync(
                "AircraftConnected",
                new ConnectedAircraftInfo(notification.Callsign, notification.StationId, notification.FlightSimulationNetwork, notification.DataAuthorityState),
                cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about connected aircraft {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
