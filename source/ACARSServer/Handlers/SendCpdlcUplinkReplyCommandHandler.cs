using ACARSServer.Clients;
using ACARSServer.Contracts;
using ACARSServer.Messages;
using MediatR;

namespace ACARSServer.Handlers;

public class SendCpdlcUplinkReplyCommandHandler(
    IClientManager clientManager,
    ILogger<SendCpdlcUplinkReplyCommandHandler> logger)
    : IRequestHandler<SendCpdlcUplinkReplyCommand>
{
    public async Task Handle(SendCpdlcUplinkReplyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await clientManager.GetOrCreateAcarsClient(
                request.Context.FlightSimulationNetwork,
                request.Context.StationIdentifier,
                cancellationToken);
        
            await client.Send(request.Message, cancellationToken);
            logger.LogDebug(
                "Sent CPDLC reply from {ControllerCallsign} to {PilotCallsign}",
                request.Context.Callsign,
                request.Message.Recipient);
        }
        catch (Exception ex)
        {
            logger.LogError(ex ,"Failed to send CPDLC reply");
        }
    }
}