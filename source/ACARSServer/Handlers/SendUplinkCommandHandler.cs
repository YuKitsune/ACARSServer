using ACARSServer.Clients;
using ACARSServer.Messages;
using MediatR;


namespace ACARSServer.Handlers;

public class SendUplinkCommandHandler(IClientManager clientManager, ILogger logger)
    : IRequestHandler<SendUplinkCommand>
{
    public async Task Handle(SendUplinkCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await clientManager.GetAcarsClient(
                request.Context.FlightSimulationNetwork,
                request.Context.StationIdentifier,
                cancellationToken);

            await client.Send(request.Uplink, cancellationToken);
            logger.Information(
                "Sent CPDLC message from {ControllerCallsign} to {PilotCallsign}",
                request.Context.Callsign,
                request.Uplink.Recipient);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Failed to send CPDLC message");
        }
    }
}
