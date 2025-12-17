using ACARSServer.Clients;

namespace ACARSServer.Tests.Mocks;

public class TestClientManager : IClientManager
{
    private readonly Dictionary<string, IAcarsClient> _clients = new();
    private readonly TestAcarsClient _defaultClient = new();

    public Task EnsureClientExists(string flightSimulationNetwork, string stationIdentifier, CancellationToken cancellationToken)
    {
        var key = CreateKey(flightSimulationNetwork, stationIdentifier);
        if (!_clients.ContainsKey(key))
        {
            _clients[key] = _defaultClient;
        }
        return Task.CompletedTask;
    }

    public Task<IAcarsClient> GetOrCreateAcarsClient(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken)
    {
        var key = CreateKey(flightSimulationNetwork, stationId);
        if (!_clients.TryGetValue(key, out var client))
        {
            client = _defaultClient;
            _clients[key] = client;
        }
        return Task.FromResult(client);
    }

    public Task RemoveAcarsClient(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken)
    {
        var key = CreateKey(flightSimulationNetwork, stationId);
        _clients.Remove(key);
        return Task.CompletedTask;
    }

    public bool ClientExists(string flightSimulationNetwork, string stationIdentifier)
    {
        return _clients.ContainsKey(CreateKey(flightSimulationNetwork, stationIdentifier));
    }

    private string CreateKey(string flightSimulationNetwork, string stationId) => $"{flightSimulationNetwork}/{stationId}";
}
