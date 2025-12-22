using MediatR;

namespace ACARSServer.Messages;

public record AircraftDisconnected(
    string FlightSimulationNetwork,
    string StationId,
    string Callsign)
    : INotification;