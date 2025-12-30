using System.Threading.Channels;
using ACARSServer.Contracts;
using ACARSServer.Model;

namespace ACARSServer.Clients;

public interface IAcarsClient : IAsyncDisposable
{
    ChannelReader<DownlinkMessage> MessageReader { get; }
    Task Connect(CancellationToken cancellationToken);
    Task Send(UplinkMessage message, CancellationToken cancellationToken);
    Task<string[]> ListConnections(CancellationToken cancellationToken);
    Task Disconnect(CancellationToken cancellationToken);
}