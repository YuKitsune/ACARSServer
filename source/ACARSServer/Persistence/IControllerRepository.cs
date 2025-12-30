using ACARSServer.Model;

namespace ACARSServer.Persistence;

public interface IControllerRepository
{
    Task Add(ControllerInfo controller, CancellationToken cancellationToken);
    Task<ControllerInfo?> FindByConnectionId(string connectionId, CancellationToken cancellationToken);
    Task<ControllerInfo[]> All(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken);
    Task<bool> RemoveByConnectionId(string connectionId, CancellationToken cancellationToken);
}