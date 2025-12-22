using ACARSServer.Contracts;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;

namespace ACARSServer.Handlers;

public class GetConnectedAircraftRequestHandler(IAircraftManager aircraftManager)
    : IRequestHandler<GetConnectedAircraftRequest, GetConnectedAircraftResult>
{
    public Task<GetConnectedAircraftResult> Handle(
        GetConnectedAircraftRequest request,
        CancellationToken cancellationToken)
    {
        var aircraft = aircraftManager.All(
            request.FlightSimulationNetwork,
            request.StationIdentifier);

        var aircraftInfo = aircraft
            .Select(a => new ConnectedAircraftInfo(
                a.Callsign,
                a.StationId,
                a.FlightSimulationNetwork,
                a.DataAuthorityState))
            .ToArray();

        return Task.FromResult(new GetConnectedAircraftResult(aircraftInfo));
    }
}
