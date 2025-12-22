using ACARSServer.Model;
using MediatR;

namespace ACARSServer.Messages;

public record AircraftConnected(
    string FlightSimulationNetwork,
    string StationId,
    string Callsign,
    DataAuthorityState DataAuthorityState)
    : INotification;