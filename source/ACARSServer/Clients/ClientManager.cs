using ACARSServer.Contracts;
using ACARSServer.Exceptions;
using ACARSServer.Extensions;
using ACARSServer.Infrastructure;
using ACARSServer.Messages;
using MediatR;

namespace ACARSServer.Clients;

public interface IClientManager
{
    Task EnsureClientExists(string flightSimulationNetwork, string stationIdentifier, CancellationToken cancellationToken);
    Task<IAcarsClient> GetOrCreateAcarsClient(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken);
    Task RemoveAcarsClient(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken);
}

public class ClientManager : IAsyncDisposable, IClientManager
{
    readonly HoppiesConfiguration[] _hoppieConfigurations;
    readonly SemaphoreSlim _semaphoreSlim = new(1, 1);
    readonly Dictionary<string, AcarsClientHandle> _clients = [];
    readonly IClock _clock;
    readonly ILoggerFactory _loggerFactory;
    readonly IMediator _mediator;
    readonly ILogger<ClientManager> _logger;

    bool _disposed;

    public ClientManager(
        IConfiguration configuration,
        ILoggerFactory loggerFactory,
        IMediator mediator,
        IClock clock,
        ILogger<ClientManager> logger)
    {
        var hoppiesConfigurationSection = configuration.GetSection("Hoppies");
        _hoppieConfigurations = hoppiesConfigurationSection.Get<HoppiesConfiguration[]>() ?? [];

        _loggerFactory = loggerFactory;
        _mediator = mediator;
        _clock = clock;
        _logger = logger;
    }

    public async Task EnsureClientExists(string flightSimulationNetwork, string stationIdentifier, CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.LockAsync(cancellationToken))
        {
            if (_clients.ContainsKey(CreateKey(flightSimulationNetwork, stationIdentifier)))
                return;

            var acarsClientHandle = await CreateAcarsClient(flightSimulationNetwork, stationIdentifier, cancellationToken);
            
            var key = CreateKey(flightSimulationNetwork, stationIdentifier);
            _clients.Add(key, acarsClientHandle);
        }
    }

    public async Task<IAcarsClient> GetOrCreateAcarsClient(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.LockAsync(cancellationToken))
        {
            if (!_clients.TryGetValue(CreateKey(flightSimulationNetwork, stationId), out var acarsClientHandle))
            {
                acarsClientHandle = await CreateAcarsClient(flightSimulationNetwork, stationId, cancellationToken);
            
                var key = CreateKey(flightSimulationNetwork, stationId);
                _clients.Add(key, acarsClientHandle);
            }

            return acarsClientHandle.Client;
        }
    }

    public async Task RemoveAcarsClient(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken)
    {
        using (await _semaphoreSlim.LockAsync(cancellationToken))
        {
            var key = CreateKey(flightSimulationNetwork, stationId);
            if (!_clients.Remove(key, out var acarsClientHandle))
                return;

            await acarsClientHandle.DisposeAsync();
        }
    }

    async Task<AcarsClientHandle> CreateAcarsClient(string flightSimulationNetwork, string stationId, CancellationToken cancellationToken)
    {
        var configuration = _hoppieConfigurations.FirstOrDefault(c => c.FlightSimulationNetwork == flightSimulationNetwork && c.StationIdentifier == stationId);
        if (configuration is null)
            throw new ConfigurationNotFoundException(flightSimulationNetwork, stationId);
        
        var httpClient = new HttpClient();
        httpClient.BaseAddress = configuration.Url;
            
        var acarsClient = new HoppieAcarsClient(
            configuration,
            httpClient,
            _clock,
            _loggerFactory.CreateLogger<HoppieAcarsClient>());

        var subscribeTaskCancellationSource = new CancellationTokenSource();
        var subscribeTask = Subscribe(
            flightSimulationNetwork,
            stationId,
            acarsClient,
            _mediator,
            subscribeTaskCancellationSource.Token);

        var acarsClientHandle = new AcarsClientHandle(acarsClient, subscribeTask, subscribeTaskCancellationSource);

        await acarsClient.Connect(cancellationToken);
            
        _logger.LogInformation(
            "Subscribed to Hoppies ACARS messages for {Network} to {StationIdentifier}",
            configuration.FlightSimulationNetwork,
            configuration.StationIdentifier);
        
        return acarsClientHandle;
    }

    async Task Subscribe(string flightSimulationNetwork, string stationIdentifier, IAcarsClient acarsClient, IMediator mediator, CancellationToken cancellationToken)
    {
        await foreach (var acarsMessage in acarsClient.MessageReader.ReadAllAsync(cancellationToken))
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ClientManager));

            try
            {
                switch (acarsMessage)
                {
                    case CpdlcDownlinkMessage cpdlcDownlinkMessage:
                        await mediator.Publish(
                            new CpdlcDownlinkMessageReceivedNotification(flightSimulationNetwork, stationIdentifier,
                                cpdlcDownlinkMessage),
                            cancellationToken);
                        break;

                    case CpdlcDownlinkReply cpdlcDownlinkReply:
                        await mediator.Publish(
                            new CpdlcDownlinkReplyReceivedNotification(flightSimulationNetwork, stationIdentifier,
                                cpdlcDownlinkReply),
                            cancellationToken);
                        break;

                    case TelexDownlinkMessage telexDownlinkMessage:
                        await mediator.Publish(
                            new TelexDownlinkMessageReceivedNotification(flightSimulationNetwork, stationIdentifier,
                                telexDownlinkMessage),
                            cancellationToken);
                        break;

                    default:
                        _logger.LogWarning("Unsupported message type: {Type}", acarsMessage.GetType());
                        break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Subscription canceled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to relay {MessageType}", acarsMessage.GetType());
            }
        }
    }

    record AcarsClientHandle(
        IAcarsClient Client,
        Task SubscribeTask,
        CancellationTokenSource SubscribeCancellationTokenSource) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();

            await SubscribeCancellationTokenSource.CancelAsync();
            await SubscribeTask;
            
            SubscribeCancellationTokenSource.Dispose();
            SubscribeTask.Dispose();
        }
    }
    
    string CreateKey(string flightSimulationNetwork, string stationId) => $"{flightSimulationNetwork}/{stationId}";

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        
        _disposed = true;
        
        _loggerFactory.Dispose();

        foreach (var (_, clientHandle) in _clients)
        {
            await clientHandle.DisposeAsync();
        }
        
        _clients.Clear();
    }
}