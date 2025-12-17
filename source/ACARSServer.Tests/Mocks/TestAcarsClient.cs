using System.Threading.Channels;
using ACARSServer.Clients;
using ACARSServer.Contracts;

namespace ACARSServer.Tests.Mocks;

public class TestAcarsClient : IAcarsClient
{
    private readonly Channel<IAcarsMessage> _channel = Channel.CreateUnbounded<IAcarsMessage>();

    public ChannelReader<IAcarsMessage> MessageReader => _channel.Reader;

    public List<IAcarsMessage> SentMessages { get; } = new();

    public Task Connect(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task Send(IAcarsMessage message, CancellationToken cancellationToken)
    {
        SentMessages.Add(message);
        return Task.CompletedTask;
    }

    public Task Disconnect(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
