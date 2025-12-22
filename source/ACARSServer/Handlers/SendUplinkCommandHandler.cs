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
            request.FlightSimulationNetwork,
            request.StationIdentifier,
            cancellationToken);
        
        var messageId = await messageIdProvider.GetNextMessageId(
            request.StationIdentifier,
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
            "Sent CPDLC message from {Sender} to {PilotCallsign}",
            request.Sender,
            uplink.Recipient);

        return new SendUplinkResult(uplink);
    }
}
