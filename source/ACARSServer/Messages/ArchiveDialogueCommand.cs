using MediatR;

namespace ACARSServer.Messages;

public record ArchiveDialogueCommand(Guid DialogueId) : IRequest;
