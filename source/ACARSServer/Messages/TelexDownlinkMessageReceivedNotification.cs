using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record TelexDownlinkMessageReceivedNotification(
    string FlightSimulationNetwork,
    string StationIdentifier,
    TelexDownlink Downlink)
    : INotification;
