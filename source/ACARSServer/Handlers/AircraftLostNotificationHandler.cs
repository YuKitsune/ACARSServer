using ACARSServer.Contracts;
using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Persistence;
using ACARSServer.Services;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Handlers;

public class AircraftLostNotificationHandler(
    IAircraftRepository aircraftRepository,
    IControllerRepository controllerRepository,
    IHubContext<ControllerHub> hubContext,
    IMessageIdProvider messageIdProvider,
    ILogger logger)
    : INotificationHandler<AircraftLost>
{
    public async Task Handle(AircraftLost notification, CancellationToken cancellationToken)
    {
        var aircraft = await aircraftRepository.Find(
            notification.FlightSimulationNetwork,
            notification.StationId,
            notification.Callsign,
            cancellationToken);

        if (aircraft is null)
        {
            logger.Information(
                "Aircraft {Callsign} already removed from tracking on {Network}/{StationId}",
                notification.Callsign,
                notification.FlightSimulationNetwork,
                notification.StationId);
            return;
        }

        // Remove aircraft from tracking
        await aircraftRepository.Remove(
            notification.FlightSimulationNetwork,
            notification.StationId,
            notification.Callsign,
            cancellationToken);

        logger.Information(
            "Aircraft {Callsign} lost on {Network}/{StationId}",
            notification.Callsign,
            notification.FlightSimulationNetwork,
            notification.StationId);

        // Find all controllers on the same network and station
        var controllers = await controllerRepository.All(
            notification.FlightSimulationNetwork,
            notification.StationId, cancellationToken);

        if (!controllers.Any())
        {
            logger.Information(
                "No controllers to notify about lost aircraft {Callsign}",
                notification.Callsign);
            return;
        }

        // Create error downlink message
        var messageId = await messageIdProvider.GetNextMessageId(
            notification.StationId,
            notification.Callsign,
            cancellationToken);

        var errorDownlink = new CpdlcDownlink(
            messageId,
            notification.Callsign,
            null,
            CpdlcDownlinkResponseType.NoResponse,
            "ERROR CONNECTION TIMED OUT");

        var controllerConnectionIds = controllers.Select(c => c.ConnectionId).ToArray();

        // Send the error downlink to controllers
        await hubContext.Clients
            .Clients(controllerConnectionIds)
            .SendAsync("DownlinkReceived", errorDownlink, cancellationToken);

        // Notify controllers that the aircraft has disconnected
        await hubContext.Clients
            .Clients(controllerConnectionIds)
            .SendAsync("AircraftDisconnected", notification.Callsign, cancellationToken);

        logger.Information(
            "Notified {ControllerCount} controller(s) about lost aircraft {Callsign}",
            controllers.Length,
            notification.Callsign);
    }
}
