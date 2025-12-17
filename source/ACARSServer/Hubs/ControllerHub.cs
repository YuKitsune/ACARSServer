using ACARSServer.Contracts;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Hubs;

public class ControllerHub : Hub
{
    private readonly IControllerManager _controllerManager;
    private readonly IMediator _mediator;
    private readonly ILogger<ControllerHub> _logger;

    public ControllerHub(
        IControllerManager controllerManager,
        IMediator mediator,
        ILogger<ControllerHub> logger)
    {
        _controllerManager = controllerManager;
        _mediator = mediator;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext is null)
        {
            throw new HubException("HTTP context not available");
        }

        var query = httpContext.Request.Query;
        var network = query["network"].ToString();
        var stationId = query["stationId"].ToString();
        var callsign = query["callsign"].ToString();

        if (string.IsNullOrWhiteSpace(network) ||
            string.IsNullOrWhiteSpace(stationId) ||
            string.IsNullOrWhiteSpace(callsign))
        {
            throw new HubException("Required parameters missing: network, stationId, and callsign must be provided");
        }

        var controller = new ControllerInfo(
            Guid.NewGuid(), // TODO: Source from database
            Context.ConnectionId,
            network,
            stationId,
            callsign);

        _controllerManager.AddController(controller);

        _logger.LogInformation(
            "Controller connected: {Callsign} on {Network}/{StationId} (ConnectionId: {ConnectionId})",
            callsign, network, stationId, Context.ConnectionId);

        await _mediator.Publish(
            new ControllerConnectedNotification(
                controller.UserId,
                controller.FlightSimulationNetwork,
                controller.Callsign,
                controller.StationIdentifier));

        await base.OnConnectedAsync();
    }

    public async Task SendCpdlcMessage(CpdlcUplinkMessage uplinkMessage)
    {
        var controller = _controllerManager.GetController(Context.ConnectionId);
        if (controller is null)
        {
            _logger.LogWarning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            return;
        }

        var userContext = new UserContext(
            controller.UserId,
            controller.ConnectionId,
            controller.FlightSimulationNetwork,
            controller.StationIdentifier,
            controller.Callsign);

        await _mediator.Send(new SendCpdlcUplinkMessageCommand(userContext, uplinkMessage));
    }

    public async Task SendCpdlcReply(CpdlcUplinkReply uplinkReply)
    {
        var controller = _controllerManager.GetController(Context.ConnectionId);
        if (controller is null)
        {
            _logger.LogWarning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            return;
        }

        var userContext = new UserContext(
            controller.UserId,
            controller.ConnectionId,
            controller.FlightSimulationNetwork,
            controller.StationIdentifier,
            controller.Callsign);

        await _mediator.Send(new SendCpdlcUplinkReplyCommand(userContext, uplinkReply));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var controller = _controllerManager.GetController(Context.ConnectionId);
        if (controller is not null)
        {
            _controllerManager.RemoveController(Context.ConnectionId);
            _logger.LogInformation(
                "Controller disconnected: {Callsign} (ConnectionId: {ConnectionId})",
                controller.Callsign, Context.ConnectionId);

            await _mediator.Publish(
                new ControllerDisconnectedNotification(
                    controller.UserId,
                    controller.FlightSimulationNetwork,
                    controller.StationIdentifier,
                    controller.Callsign));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
