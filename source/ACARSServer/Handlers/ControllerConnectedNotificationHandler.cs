using ACARSServer.Clients;
using ACARSServer.Messages;
using MediatR;

namespace ACARSServer.Handlers;

public class ControllerConnectedNotificationHandler(
    IClientManager clientManager,
    ILogger<ControllerConnectedNotificationHandler> logger)
    : INotificationHandler<ControllerConnectedNotification>
{
    public async Task Handle(ControllerConnectedNotification notification, CancellationToken cancellationToken)
    {
        await clientManager.EnsureClientExists(notification.FlightSimulationNetwork, notification.StationIdentifier, cancellationToken);
        logger.LogInformation(
            "{Callsign} on {Network} connected to {Station}",
            notification.Callsign,
            notification.FlightSimulationNetwork,
            notification.StationIdentifier);
    }
}