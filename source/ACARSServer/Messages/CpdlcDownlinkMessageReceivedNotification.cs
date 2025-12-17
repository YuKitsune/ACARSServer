using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record CpdlcDownlinkMessageReceivedNotification(
    string FlightSimulationNetwork,
    string StationIdentifier,
    CpdlcDownlinkMessage Message)
    : INotification;

public record CpdlcDownlinkReplyReceivedNotification(
    string FlightSimulationNetwork,
    string StationIdentifier,
    CpdlcDownlinkReply Message)
    : INotification;