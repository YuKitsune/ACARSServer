using ACARSServer.Clients;
using ACARSServer.Messages;
using MediatR;

namespace ACARSServer.Handlers;

public class SendCpdlcUplinkCommandHandler(
    IClientManager clientManager,
    ILogger<SendCpdlcUplinkCommandHandler> logger)
    : IRequestHandler<SendCpdlcUplinkCommand>
{
    public async Task Handle(SendCpdlcUplinkCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await clientManager.GetOrCreateAcarsClient(
                request.Context.FlightSimulationNetwork,
                request.Context.StationIdentifier,
                cancellationToken);
        
            await client.Send(request.Uplink, cancellationToken);
            logger.LogDebug(
                "Sent CPDLC message from {ControllerCallsign} to {PilotCallsign}",
                request.Context.Callsign,
                request.Uplink.Recipient);
        }
        catch (Exception ex)
        {
            logger.LogError(ex ,"Failed to send CPDLC message");
        }
    }
}
