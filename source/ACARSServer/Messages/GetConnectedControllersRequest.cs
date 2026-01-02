using ACARSServer.Contracts;
using MediatR;

namespace ACARSServer.Messages;

public record GetConnectedControllersRequest(string FlightSimulationNetwork, string StationIdentifier)
    : IRequest<GetConnectedControllersResult>;

public record GetConnectedControllersResult(ControllerConnectionDto[] Controllers);
