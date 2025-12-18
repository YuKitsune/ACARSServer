using System.Threading.Channels;
using ACARSServer.Contracts;

namespace ACARSServer.Clients;

public interface IAcarsClient : IAsyncDisposable
{
    ChannelReader<IDownlinkMessage> MessageReader { get; }
    Task Connect(CancellationToken cancellationToken);
    Task Send(IUplinkMessage message, CancellationToken cancellationToken);
    Task Disconnect(CancellationToken cancellationToken);
}