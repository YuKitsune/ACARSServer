using MediatR;

namespace ACARSServer.Messages;

public record ControllerConnectedNotification(
    Guid UserId,
    string FlightSimulationNetwork,
    string Callsign,
    string StationIdentifier)
    : INotification;