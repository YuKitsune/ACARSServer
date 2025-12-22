using MediatR;

namespace ACARSServer.Messages;

public record LogonCommand(
    int DownlinkId,
    string Callsign,
    string StationId,
    string FlightSimulationNetwork) : IRequest;