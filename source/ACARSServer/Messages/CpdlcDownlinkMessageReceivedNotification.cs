using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record CpdlcDownlinkMessageReceivedNotification(
    string FlightSimulationNetwork,
    string StationIdentifier,
    ICpdlcDownlink Downlink)
    : INotification;