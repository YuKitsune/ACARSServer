using ACARSServer.Contracts;
using ACARSServer.Infrastructure;
using ACARSServer.Messages;
using ACARSServer.Model;
using MediatR;

namespace ACARSServer.Handlers;

public class LogonCommandHandler(IAircraftManager aircraftManager, IClock clock, IMediator mediator)
    : IRequestHandler<LogonCommand>
{
    public async Task Handle(LogonCommand request, CancellationToken cancellationToken)
    {
        var aircraft = new AircraftConnection(
            request.Callsign,
            request.StationId,
            request.FlightSimulationNetwork,
            DataAuthorityState.NextDataAuthority);
        
        aircraft.RequestLogon(clock.UtcNow());
        
        // TODO: Perform validation
        // TODO: What if there are no controllers online?
        
        aircraftManager.Add(aircraft);

        // Immediately accept it for now
        aircraft.AcceptLogon(clock.UtcNow());
        
        await mediator.Send(
            new SendUplinkCommand(
                "SYSTEM",
                request.FlightSimulationNetwork,
                request.StationId,
                request.Callsign,
                request.DownlinkId,
                CpdlcUplinkResponseType.NoResponse,
                "LOGON ACCEPTED"),
            cancellationToken);

        await mediator.Publish(
            new AircraftConnected(
                request.FlightSimulationNetwork,
                request.StationId,
                request.Callsign,
                aircraft.DataAuthorityState),
            cancellationToken);
    }
}