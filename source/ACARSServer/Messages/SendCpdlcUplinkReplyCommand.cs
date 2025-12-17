using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record SendCpdlcUplinkReplyCommand(UserContext Context, CpdlcUplinkReply Message) : IRequest;