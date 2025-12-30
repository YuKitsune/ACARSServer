using ACARSServer.Contracts;
using ACARSServer.Messages;
using ACARSServer.Persistence;
using MediatR;

namespace ACARSServer.Handlers;

public class GetConnectedAircraftRequestHandler(IAircraftRepository aircraftRepository)
    : IRequestHandler<GetConnectedAircraftRequest, GetConnectedAircraftResult>
{
    public async Task<GetConnectedAircraftResult> Handle(
        GetConnectedAircraftRequest request,
        CancellationToken cancellationToken)
    {
        var aircraft = await aircraftRepository.All(
            request.FlightSimulationNetwork,
            request.StationIdentifier,
            cancellationToken);

        var aircraftInfo = aircraft
            .Select(a => new ConnectedAircraftInfo(
                a.Callsign,
                a.StationId,
                a.FlightSimulationNetwork,
                a.DataAuthorityState))
            .ToArray();

        return new GetConnectedAircraftResult(aircraftInfo);
    }
}
