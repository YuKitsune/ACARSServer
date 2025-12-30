using ACARSServer.Model;
using MediatR;

namespace ACARSServer.Messages;

public record DialogueChangedNotification(Dialogue Dialogue) : INotification;