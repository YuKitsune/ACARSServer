using ACARSServer.Contracts;
using ACARSServer.Messages;
using ACARSServer.Model;
using ACARSServer.Persistence;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using CpdlcUplinkResponseType = ACARSServer.Contracts.CpdlcUplinkResponseType;

namespace ACARSServer.Hubs;

public class ControllerHub(
    IControllerRepository controllerRepository,
    IMediator mediator,
    ILogger logger)
    : Hub
{
    private readonly ILogger _logger = logger.ForContext<ControllerHub>();

    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext is null)
        {
            throw new HubException("HTTP context not available");
        }

        // Read connection parameters from query string
        var query = httpContext.Request.Query;
        var network = query["network"].ToString().ToUpper();
        var stationId = query["stationId"].ToString().ToUpper();
        var callsign = query["callsign"].ToString().ToUpper();

        if (string.IsNullOrWhiteSpace(network) ||
            string.IsNullOrWhiteSpace(stationId) ||
            string.IsNullOrWhiteSpace(callsign))
        {
            throw new HubException("Required parameters missing: network, stationId, and callsign must be provided");
        }

        // Validate API key
        // var validationResult = await _apiKeyValidator.ValidateAsync(apiKey);
        // if (validationResult is null)
        // {
        //     _logger.Warning("Invalid API key attempt from {ConnectionId}", Context.ConnectionId);
        //     throw new HubException("Invalid API key");
        // }

        var controller = new ControllerInfo(
            Guid.NewGuid(),
            Context.ConnectionId,
            network,
            stationId,
            callsign,
            "TEST");
            // validationResult.VatsimCid);

        await controllerRepository.Add(controller, Context.GetHttpContext()?.RequestAborted ?? CancellationToken.None);

        _logger.Information(
            "Controller connected: {Callsign} (VATSIM CID: {VatsimCid}) on {Network}/{StationId} (ConnectionId: {ConnectionId})",
            callsign, "TEST", network, stationId, Context.ConnectionId);

        await mediator.Publish(
            new ControllerConnectedNotification(
                controller.UserId,
                controller.FlightSimulationNetwork,
                controller.Callsign,
                controller.StationIdentifier));

        await base.OnConnectedAsync();
    }

    public async Task<SendUplinkResult> SendUplink(
        string recipient,
        int? replyToDownlinkId,
        CpdlcUplinkResponseType responseType,
        string content)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        // TODO: Move to converter
        var modelResponseType = responseType switch
        {
            CpdlcUplinkResponseType.NoResponse => Model.CpdlcUplinkResponseType.NoResponse,
            CpdlcUplinkResponseType.WilcoUnable => Model.CpdlcUplinkResponseType.WilcoUnable,
            CpdlcUplinkResponseType.AffirmativeNegative => Model.CpdlcUplinkResponseType.AffirmativeNegative,
            CpdlcUplinkResponseType.Roger => Model.CpdlcUplinkResponseType.Roger,
            _ => throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null)
        };

        var command = new SendUplinkCommand(
            controller.Callsign,
            controller.FlightSimulationNetwork,
            controller.StationIdentifier,
            recipient,
            replyToDownlinkId,
            modelResponseType,
            content);

        return await mediator.Send(command);
    }

    public async Task<GetConnectedAircraftResult> GetConnectedAircraft()
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is null)
        {
            _logger.Warning("Controller not found for connection {ConnectionId}", Context.ConnectionId);
            throw new InvalidOperationException($"Controller not found for connection {Context.ConnectionId}");
        }

        var query = new GetConnectedAircraftRequest(controller.FlightSimulationNetwork, controller.StationIdentifier);
        return await mediator.Send(query);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var controller = await controllerRepository.FindByConnectionId(Context.ConnectionId, CancellationToken.None);
        if (controller is not null)
        {
            await controllerRepository.RemoveByConnectionId(Context.ConnectionId,  CancellationToken.None);
            _logger.Information(
                "Controller disconnected: {Callsign} (ConnectionId: {ConnectionId})",
                controller.Callsign, Context.ConnectionId);

            await mediator.Publish(
                new ControllerDisconnectedNotification(
                    controller.UserId,
                    controller.FlightSimulationNetwork,
                    controller.StationIdentifier,
                    controller.Callsign));
        }

        await base.OnDisconnectedAsync(exception);
    }
}
