using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record SendUplinkCommand(
    string Sender,
    string FlightSimulationNetwork,
    string StationIdentifier,
    string Recipient,
    int? ReplyToDownlinkId,
    CpdlcUplinkResponseType ResponseType,
    string Content)
    : IRequest<SendUplinkResult>;
    
public record SendUplinkResult(CpdlcUplink UplinkMessage);