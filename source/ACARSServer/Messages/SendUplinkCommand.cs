using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record SendUplinkCommand(
    UserContext Context,
    string Recipient,
    int? ReplyToDownlinkId,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest<SendUplinkResult>;
    
public record SendUplinkResult(int UplinkMessageId);