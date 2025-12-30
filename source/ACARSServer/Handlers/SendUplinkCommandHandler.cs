using ACARSServer.Clients;
using ACARSServer.Infrastructure;
using ACARSServer.Messages;
using ACARSServer.Model;
using ACARSServer.Persistence;
using ACARSServer.Services;
using MediatR;


namespace ACARSServer.Handlers;

public class SendUplinkCommandHandler(
    IClientManager clientManager,
    IMessageIdProvider messageIdProvider,
    IDialogueRepository dialogueRepository,
    IClock clock,
    ILogger logger)
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

        var uplinkMessage = new UplinkMessage(
            messageId,
            request.ReplyToDownlinkId,
            request.Recipient,
            request.ResponseType,
            AlertType.None,
            request.Content,
            clock.UtcNow());

        // Add or update the dialogue
        var dialogue = request.ReplyToDownlinkId.HasValue
            ? await dialogueRepository.FindDialogueForMessage(
                request.FlightSimulationNetwork,
                request.StationIdentifier,
                request.Recipient,
                request.ReplyToDownlinkId.Value,
                cancellationToken)
            : null;

        if (dialogue is null)
        {
            dialogue = new Dialogue(
                request.FlightSimulationNetwork,
                request.StationIdentifier,
                request.Recipient,
                uplinkMessage);
            await dialogueRepository.Add(dialogue, cancellationToken);
        }
        else
        {
            dialogue.AddMessage(uplinkMessage);
        }

        await client.Send(uplinkMessage, cancellationToken);
        logger.Information(
            "Sent CPDLC message from {Sender} to {PilotCallsign}",
            request.Sender,
            uplinkMessage.Recipient);

        return new SendUplinkResult(uplinkMessage);
    }
}
