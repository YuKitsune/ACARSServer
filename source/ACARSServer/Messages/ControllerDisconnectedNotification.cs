using MediatR;

namespace ACARSServer.Messages;

public record ControllerDisconnectedNotification(
    Guid UserId,
    string FlightSimulationNetwork,
    string StationIdentifier,
    string Callsign)
    : INotification;
