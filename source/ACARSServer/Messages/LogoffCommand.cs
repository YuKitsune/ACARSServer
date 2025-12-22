using MediatR;

namespace ACARSServer.Messages;

public record LogoffCommand(
    int DownlinkId,
    string Callsign,
    string StationId,
    string FlightSimulationNetwork) : IRequest;