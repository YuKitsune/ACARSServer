using MediatR;

namespace ACARSServer.Messages;

public record AcknowledgeUplinkCommand(Guid DialogueId, int UplinkMessageId) : IRequest;