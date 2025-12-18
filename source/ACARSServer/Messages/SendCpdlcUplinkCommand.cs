using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record SendCpdlcUplinkCommand(
    UserContext Context,
    ICpdlcUplink Uplink)
    : IRequest;