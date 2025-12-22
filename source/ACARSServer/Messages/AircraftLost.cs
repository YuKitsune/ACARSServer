using MediatR;

namespace ACARSServer.Messages;

public record AircraftLost(
    string FlightSimulationNetwork,
    string StationId,
    string Callsign)
    : INotification;