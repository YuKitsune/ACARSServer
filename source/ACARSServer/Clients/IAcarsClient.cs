using System.Threading.Channels;
using ACARSServer.Contracts;

namespace ACARSServer.Clients;

public interface IAcarsClient : IAsyncDisposable
{
    ChannelReader<IAcarsMessage> MessageReader { get; }
    Task Connect(CancellationToken cancellationToken);
    Task Send(IAcarsMessage message, CancellationToken cancellationToken);
    Task Disconnect(CancellationToken cancellationToken);
}