using ACARSServer.Contracts;
using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Handlers;



public class CpdlcMessageReceivedNotificationHandler : INotificationHandler<CpdlcDownlinkMessageReceivedNotification>
{
    private readonly IControllerManager _controllerManager;
    private readonly IHubContext<ControllerHub> _hubContext;
    private readonly ILogger<CpdlcMessageReceivedNotificationHandler> _logger;

    public CpdlcMessageReceivedNotificationHandler(
        IControllerManager controllerManager,
        IHubContext<ControllerHub> hubContext,
        ILogger<CpdlcMessageReceivedNotificationHandler> logger)
    {
        _controllerManager = controllerManager;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Handle(CpdlcDownlinkMessageReceivedNotification notification, CancellationToken cancellationToken)
    {
        var controllers = _controllerManager.Controllers
            .Where(c =>
                c.FlightSimulationNetwork == notification.FlightSimulationNetwork &&
                c.StationIdentifier == notification.StationIdentifier)
            .ToArray();

        if (!controllers.Any())
        {
            _logger.LogInformation("No controllers found for message from {From}", notification.Downlink.Sender);
            return;
        }

        // Broadcast to all controllers connected to the station
        // TODO: Filter by jurisdiction.
        //  Plugin needs to ignore messages from flights not assumed.
        await _hubContext.Clients
            .Clients(controllers.Select(c => c.ConnectionId))
            .SendAsync("ReceiveCpdlcMessage", notification.Downlink, cancellationToken);

        _logger.LogInformation("Relayed CPDLC message from {From} to {StationIdentifier}", notification.Downlink.Sender, notification.StationIdentifier);
    }
}