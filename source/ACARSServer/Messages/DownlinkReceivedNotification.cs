using ACARSServer.Model;
using MediatR;

namespace ACARSServer.Messages;

public record DownlinkReceivedNotification(
    string FlightSimulationNetwork,
    string StationIdentifier,
    DownlinkMessage Downlink)
    : INotification;
