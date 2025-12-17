using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record SendCpdlcUplinkMessageCommand(UserContext Context, CpdlcUplinkMessage Message) : IRequest;