using ACARSServer.Clients;
using ACARSServer.Contracts;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;

namespace ACARSServer.Handlers;

public class ControllerDisconnectedNotificationHandler(
    IControllerManager controllerManager,
    IClientManager clientManager,
    ILogger<ControllerDisconnectedNotificationHandler> logger)
    : INotificationHandler<ControllerDisconnectedNotification>
{
    public async Task Handle(ControllerDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        var controllersRemain = controllerManager.Controllers
            .Any(c =>
                c.FlightSimulationNetwork == notification.FlightSimulationNetwork &&
                c.StationIdentifier == notification.StationIdentifier);

        if (!controllersRemain)
        {
            await clientManager.RemoveAcarsClient(notification.FlightSimulationNetwork, notification.StationIdentifier, cancellationToken);
            logger.LogInformation("No controllers remaining for {Station} on {Network}. ACARS client terminated.", notification.StationIdentifier, notification.FlightSimulationNetwork);
        }
    }
}