using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record GetConnectedAircraftRequest(string FlightSimulationNetwork, string StationIdentifier)
    : IRequest<GetConnectedAircraftResult>;

public record GetConnectedAircraftResult(ConnectedAircraftInfo[] Aircraft);
