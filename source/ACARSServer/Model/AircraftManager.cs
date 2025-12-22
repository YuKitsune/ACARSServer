using System.Collections.Concurrent;

namespace ACARSServer.Model;

public interface IAircraftManager
{
    void Add(AircraftConnection connection);
    AircraftConnection? Get(string flightSimulationNetwork, string stationId, string callsign);
    AircraftConnection[] All(string flightSimulationNetwork, string stationId);
    bool Remove(string flightSimulationNetwork, string stationId, string callsign);
}

public class AircraftManager : IAircraftManager
{
    record Key(string FlightSimulationNetwork, string StationId, string Callsign);
    
    private readonly ConcurrentDictionary<Key, AircraftConnection> _connections = new();

    public void Add(AircraftConnection connection)
    {
        var key = new Key(connection.FlightSimulationNetwork, connection.StationId, connection.Callsign);
        _connections[key] = connection;
    }
    
    public AircraftConnection? Get(string flightSimulationNetwork, string stationId, string callsign)
    {
        var key = new Key(flightSimulationNetwork, stationId, callsign);
        _connections.TryGetValue(key, out var connection);
        return connection;
    }

    public AircraftConnection[] All(string flightSimulationNetwork, string stationId)
    {
        return _connections
            .Where(kvp => kvp.Key.FlightSimulationNetwork == flightSimulationNetwork && kvp.Key.StationId == stationId)
            .Select(kvp => kvp.Value)
            .ToArray();
    }

    public bool Remove(string flightSimulationNetwork, string stationId, string callsign)
    {
        var key = new Key(flightSimulationNetwork, stationId, callsign);
        return _connections.TryRemove(key, out _);
    }
}