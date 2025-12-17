using ACARSServer.Contracts;
using ACARSServer.Messages;
using ACARSServer.Model;
using ACARSServer.Services;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Hubs;

public class ControllerHub : Hub
{
    private readonly IControllerManager _controllerManager;
    private readonly IMediator _mediator;
    private readonly ILogger<ControllerHub> _logger;
    private readonly IApiKeyValidator _apiKeyValidator;

    public ControllerHub(
        IControllerManager controllerManager,
        IMediator mediator,
        ILogger<ControllerHub> logger,
        IApiKeyValidator apiKeyValidator)
    {
        _controllerManager = controllerManager;
        _mediator = mediator;
        _logger = logger;
        _apiKeyValidator = apiKeyValidator;
    }

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext is null)
        {
            throw new HubException("HTTP context not available");
        }

        // Read API key from header
        var apiKey = httpContext.Request.Headers["X-ACARS-ApiKey"].ToString();

        // Read connection parameters from query string
        var query = httpContext.Request.Query;
        var network = query["network"].ToString();
        var stationId = query["stationId"].ToString();
        var callsign = query["callsign"].ToString();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new HubException("API key is required (provide X-ACARS-ApiKey header)");
        }

        if (string.IsNullOrWhiteSpace(network) ||
            string.IsNullOrWhiteSpace(stationId) ||
            string.IsNullOrWhiteSpace(callsign))
        {
            throw new HubException("Required parameters missing: network, stationId, and callsign must be provided");
        }

        // Validate API key
        var validationResult = await _apiKeyValidator.ValidateAsync(apiKey);
        if (validationResult is null)
        {
            _logger.LogWarning("Invalid API key attempt from {ConnectionId}", Context.ConnectionId);
            throw new HubException("Invalid API key");
        }

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            Context.ConnectionId,
            network,
            stationId,
            callsign,
            validationResult.VatsimCid);

        _controllerManager.AddController(controller);

        _logger.LogInformation(
            "Controller connected: {Callsign} (VATSIM CID: {VatsimCid}) on {Network}/{StationId} (ConnectionId: {ConnectionId})",
            callsign, validationResult.VatsimCid, network, stationId, Context.ConnectionId);

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
