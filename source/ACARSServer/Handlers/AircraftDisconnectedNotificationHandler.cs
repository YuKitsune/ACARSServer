using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Handlers;

public class AircraftDisconnectedNotificationHandler(
    IControllerManager controllerManager,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<AircraftDisconnected>
{
    public async Task Handle(AircraftDisconnected notification, CancellationToken cancellationToken)
    {
        logger.Information(
            "Aircraft {Callsign} disconnected from {Network}/{StationId}",
            notification.Callsign,
            notification.FlightSimulationNetwork,
            notification.StationId);

        // Find all controllers on the same network and station
        var controllers = controllerManager.Controllers
            .Where(c =>
                c.FlightSimulationNetwork == notification.FlightSimulationNetwork &&
                c.StationIdentifier == notification.StationId)
            .ToArray();

        if (!controllers.Any())
        {
            logger.Information(
                "No controllers to notify about disconnected aircraft {Callsign}",
                notification.Callsign);
            return;
        }

        // Notify all controllers that an aircraft has disconnected
        await hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("AircraftDisconnected", notification.Callsign, cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about disconnected aircraft {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
