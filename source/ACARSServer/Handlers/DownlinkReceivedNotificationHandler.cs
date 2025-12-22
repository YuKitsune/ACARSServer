using ACARSServer.Contracts;
using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Handlers;

public class DownlinkReceivedNotificationHandler(
    IAircraftManager aircraftManager,
    IMediator mediator,
    IControllerManager controllerManager,
    IHubContext<ControllerHub> hubContext,
    ILogger logger)
    : INotificationHandler<DownlinkReceivedNotification>
{
    public async Task Handle(DownlinkReceivedNotification notification, CancellationToken cancellationToken)
    {
        // Intercept logon requests and automatically respond
        if (ControlMessages.IsLogonRequest(notification.Downlink))
        {
            await mediator.Send(
                new LogonCommand(
                    notification.Downlink.Id,
                    notification.Downlink.Sender,
                    notification.StationIdentifier,
                    notification.FlightSimulationNetwork),
                cancellationToken);
            return;
        }

        var aircraftConnection = aircraftManager.Get(
            notification.FlightSimulationNetwork,
            notification.StationIdentifier,
            notification.Downlink.Sender);
        if (aircraftConnection is null)
        {
            // Connection not known, reject.
            await mediator.Send(
                new SendUplinkCommand(
                    "SYSTEM",
                    notification.FlightSimulationNetwork,
                    notification.StationIdentifier,
                    notification.Downlink.Sender,
                    notification.Downlink.Id,
                    CpdlcUplinkResponseType.NoResponse,
                    "ERROR. CONNECTION NOT ESTABLISHED."),
                cancellationToken);
            return;
        }
        
        // Intercept logoff messages
        if (ControlMessages.IsLogoffNotice(notification.Downlink))
        {
            await mediator.Send(
                new LogoffCommand(
                    notification.Downlink.Id,
                    notification.Downlink.Sender,
                    notification.StationIdentifier,
                    notification.FlightSimulationNetwork),
                cancellationToken);

            // Allow these to flow through to the controller
        }

        // Promote aircraft to CurrentDataAuthority on first downlink
        if (aircraftConnection.DataAuthorityState == DataAuthorityState.NextDataAuthority)
        {
            aircraftConnection.PromoteToCurrentDataAuthority();
        }

        var controllers = controllerManager.Controllers
            .Where(c =>
                c.FlightSimulationNetwork == notification.FlightSimulationNetwork &&
                c.StationIdentifier == notification.StationIdentifier)
            .ToArray();

        if (controllers.Length == 0)
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