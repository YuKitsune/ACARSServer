using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;

namespace ACARSServer.Handlers;

public class LogoffCommandHandler(IAircraftManager aircraftManager, IMediator mediator)
    : IRequestHandler<LogoffCommand>
{
    public async Task Handle(LogoffCommand request, CancellationToken cancellationToken)
    {
        if (!aircraftManager.Remove(request.FlightSimulationNetwork, request.StationId, request.Callsign))
            return;

        await mediator.Publish(
            new AircraftDisconnected(
                request.FlightSimulationNetwork,
                request.StationId,
                request.Callsign),
            cancellationToken);
    }
}