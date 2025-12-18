using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record DownlinkReceivedNotification(
    string FlightSimulationNetwork,
    string StationIdentifier,
    IDownlinkMessage Downlink)
    : INotification;
