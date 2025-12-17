namespace ACARSServer.Clients;

public interface IAcarsNetworkConfiguration
{
    string FlightSimulationNetwork { get; }
    string StationIdentifier { get; }
}

public class HoppiesConfiguration : IAcarsNetworkConfiguration
{
    public required string FlightSimulationNetwork { get; init; }
    public required string StationIdentifier { get; init; } // i.e. YMMM
    public required Uri Url { get; init; }
    public required string AuthenticationCode { get; init; }
}