using ACARSServer.Model;

namespace ACARSServer.Tests.Mocks;

public class TestAircraftManager : IAircraftManager
{
    private readonly AircraftManager _inner = new();

    public void Add(AircraftConnection connection) => _inner.Add(connection);

    public AircraftConnection? Get(string flightSimulationNetwork, string stationId, string callsign)
        => _inner.Get(flightSimulationNetwork, stationId, callsign);

    public AircraftConnection[] All(string flightSimulationNetwork, string stationId)
        => _inner.All(flightSimulationNetwork, stationId);

    public bool Remove(string flightSimulationNetwork, string stationId, string callsign)
        => _inner.Remove(flightSimulationNetwork, stationId, callsign);
}
