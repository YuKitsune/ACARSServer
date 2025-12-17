namespace ACARSServer.Model;

public class ControllerInfo(
    Guid userId,
    string connectionId,
    string flightSimulationNetwork,
    string stationIdentifier,
    string callsign)
{
    public Guid UserId { get; } = userId;
    public string ConnectionId { get; } = connectionId;
    public string FlightSimulationNetwork { get; } = flightSimulationNetwork;
    public string StationIdentifier { get; } = stationIdentifier;
    public string Callsign { get; } = callsign;
}