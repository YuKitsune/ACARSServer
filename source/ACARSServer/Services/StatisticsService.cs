using ACARSServer.Model;

namespace ACARSServer.Services;

public class StatisticsService(IControllerManager controllerManager) : IStatisticsService
{
    public ServerStatistics GetCurrentStatistics()
    {
        var controllers = controllerManager.Controllers;

        var connections = controllers.Select(c => new ConnectionInfo
        {
            Network = c.FlightSimulationNetwork,
            StationId = c.StationIdentifier,
            Callsign = c.Callsign,
            VatsimCid = c.VatsimCid
        }).ToList();

        return new ServerStatistics
        {
            TotalConnections = controllers.Count,
            UniqueNetworks = controllers.Select(c => c.FlightSimulationNetwork).Distinct().Count(),
            UniqueStations = controllers.Select(c => c.StationIdentifier).Distinct().Count(),
            Connections = connections
        };
    }
}
