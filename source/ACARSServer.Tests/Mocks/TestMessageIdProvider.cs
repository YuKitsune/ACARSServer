using ACARSServer.Services;

namespace ACARSServer.Tests.Mocks;

public class TestMessageIdProvider : IMessageIdProvider
{
    private int _nextId = 1;

    public Task<int> GetNextMessageId(string stationId, string callsign, CancellationToken cancellationToken)
    {
        return Task.FromResult(_nextId++);
    }
}