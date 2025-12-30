using ACARSServer.Messages;
using ACARSServer.Persistence;
using MediatR;

namespace ACARSServer.Handlers;

public class LogoffCommandHandler(IAircraftRepository aircraftRepository, IMediator mediator)
    : IRequestHandler<LogoffCommand>
{
    public async Task Handle(LogoffCommand request, CancellationToken cancellationToken)
    {
        var didRemove = await aircraftRepository.Remove(
            request.FlightSimulationNetwork,
            request.StationId,
            request.Callsign,
            cancellationToken);
        
        if (!didRemove)
            return;

        await mediator.Publish(
            new AircraftDisconnected(
                request.FlightSimulationNetwork,
                request.StationId,
                request.Callsign),
            cancellationToken);
    }
}