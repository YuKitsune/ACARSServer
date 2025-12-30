using MediatR;

namespace ACARSServer.Messages;

public record AcknowledgeDownlinkCommand(Guid DialogueId, int DownlinkMessageId) : IRequest;
