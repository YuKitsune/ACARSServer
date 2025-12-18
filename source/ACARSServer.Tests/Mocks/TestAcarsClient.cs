using System.Threading.Channels;
using ACARSServer.Clients;
using ACARSServer.Contracts;

namespace ACARSServer.Tests.Mocks;

public class TestAcarsClient : IAcarsClient
{
    private readonly Channel<IDownlinkMessage> _channel = Channel.CreateUnbounded<IDownlinkMessage>();

    public ChannelReader<IDownlinkMessage> MessageReader => _channel.Reader;

    public List<IUplinkMessage> SentMessages { get; } = new();

    public Task Connect(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task Send(IUplinkMessage message, CancellationToken cancellationToken)
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
