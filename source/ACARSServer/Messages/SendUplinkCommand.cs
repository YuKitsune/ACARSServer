using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record SendUplinkCommand(
    UserContext Context,
    IUplinkMessage Uplink)
    : IRequest;