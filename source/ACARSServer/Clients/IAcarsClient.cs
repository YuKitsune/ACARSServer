using System.Threading.Channels;
using ACARSServer.Contracts;

namespace ACARSServer.Clients;

public interface IAcarsClient : IAsyncDisposable
{
    ChannelReader<CpdlcDownlink> MessageReader { get; }
    Task Connect(CancellationToken cancellationToken);
    Task Send(IUplinkMessage message, CancellationToken cancellationToken);
    Task<string[]> ListConnections(CancellationToken cancellationToken);
    Task Disconnect(CancellationToken cancellationToken);
}