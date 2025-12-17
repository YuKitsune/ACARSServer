using ACARSServer.Clients;
using ACARSServer.Contracts;
using ACARSServer.Messages;
using MediatR;

namespace ACARSServer.Handlers;

public class SendCpdlcUplinkMessageCommandHandler(
    IClientManager clientManager,
    ILogger<SendCpdlcUplinkMessageCommandHandler> logger)
    : IRequestHandler<SendCpdlcUplinkMessageCommand>
{
    public async Task Handle(SendCpdlcUplinkMessageCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await clientManager.GetOrCreateAcarsClient(
                request.Context.FlightSimulationNetwork,
                request.Context.StationIdentifier,
                cancellationToken);
        
            await client.Send(request.Message, cancellationToken);
            logger.LogDebug(
                "Sent CPDLC message from {ControllerCallsign} to {PilotCallsign}",
                request.Context.Callsign,
                request.Message.Recipient);
        }
        catch (Exception ex)
        {
            logger.LogError(ex ,"Failed to send CPDLC message");
        }
    }
}
