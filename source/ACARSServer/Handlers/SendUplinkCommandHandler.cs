using ACARSServer.Clients;
using ACARSServer.Contracts;
using ACARSServer.Messages;
using ACARSServer.Services;
using MediatR;


namespace ACARSServer.Handlers;

public class SendUplinkCommandHandler(IClientManager clientManager, IMessageIdProvider messageIdProvider, ILogger logger)
    : IRequestHandler<SendUplinkCommand, SendUplinkResult>
{
    public async Task<SendUplinkResult> Handle(SendUplinkCommand request, CancellationToken cancellationToken)
    {
        var client = await clientManager.GetAcarsClient(
            request.Context.FlightSimulationNetwork,
            request.Context.StationIdentifier,
            cancellationToken);
        
        var messageId = await messageIdProvider.GetNextMessageId(
            request.Context.StationIdentifier,
            request.Recipient,
            cancellationToken);

        var uplink = new CpdlcUplink(
            messageId,
            request.Recipient,
            request.ReplyToDownlinkId,
            request.ResponseType,
            request.Content);

        await client.Send(uplink, cancellationToken);
        logger.Information(
            "Sent CPDLC message from {ControllerCallsign} to {PilotCallsign}",
            request.Context.Callsign,
            uplink.Recipient);

        return new SendUplinkResult(messageId);
    }
}
