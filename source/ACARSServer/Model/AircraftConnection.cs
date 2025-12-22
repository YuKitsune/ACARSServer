namespace ACARSServer.Model;

public class AircraftConnection(
    string callsign,
    string stationId,
    string flightSimulationNetwork,
    DataAuthorityState dataAuthorityState)
{
    public string Callsign { get; } = callsign;
    public string StationId { get; } = stationId;
    public string FlightSimulationNetwork { get; } = flightSimulationNetwork;

    public DataAuthorityState DataAuthorityState { get; private set; } = dataAuthorityState;
    public ConnectionState ConnectionState { get; private set; }

    public DateTimeOffset LogonRequested { get; private set; }
    public DateTimeOffset? LogonAccepted { get; private set; }

    public void RequestLogon(DateTimeOffset now)
    {
        ConnectionState = ConnectionState.Pending;
        LogonRequested = now;
        LogonAccepted = null;
    }

    public void AcceptLogon(DateTimeOffset now)
    {
        ConnectionState = ConnectionState.Connected;
        LogonAccepted = now;
    }

    public void PromoteToCurrentDataAuthority()
    {
        DataAuthorityState = DataAuthorityState.CurrentDataAuthority;
    }
}