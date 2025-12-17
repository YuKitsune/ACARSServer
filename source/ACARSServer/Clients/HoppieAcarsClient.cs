using System.Text.Encodings.Web;
using System.Threading.Channels;
using ACARSServer.Contracts;
using ACARSServer.Exceptions;
using ACARSServer.Infrastructure;

namespace ACARSServer.Clients;

// TODO: ADS-C

public class HoppieAcarsClient : IAcarsClient
{
    readonly HoppiesConfiguration _configuration;
    readonly HttpClient _httpClient;
    readonly IClock _clock;
    readonly ILogger<HoppieAcarsClient> _logger;
    
    readonly Channel<IAcarsMessage> _messageChannel = Channel.CreateUnbounded<IAcarsMessage>();
    readonly Random _random = Random.Shared;

    CancellationTokenSource? _pollCancellationTokenSource;
    Task? _pollTask;
    DateTimeOffset? _lastSendTime;

    bool _disposed;

    public ChannelReader<IAcarsMessage> MessageReader => _messageChannel.Reader;

    public HoppieAcarsClient(HoppiesConfiguration configuration, HttpClient httpClient, IClock clock, ILogger<HoppieAcarsClient> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _clock = clock;
        _logger = logger;
    }

    public Task Connect(CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HoppieAcarsClient));

        _pollCancellationTokenSource = new CancellationTokenSource();
        _pollTask = Poll(_pollCancellationTokenSource.Token);

        _logger.LogInformation(
            "Connected to Hoppies ACARS network for {Network} as {StationIdentifier}",
            _configuration.FlightSimulationNetwork,
            _configuration.StationIdentifier);

        return Task.CompletedTask;
    }

    public async Task Send(IAcarsMessage message, CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HoppieAcarsClient));

        var messageType = GetMessageType(message);
        var to = GetTo(message);
        var packet = GetPacket(message);

        var parameters = new Dictionary<string, string>
        {
            ["logon"] = _configuration.AuthenticationCode,
            ["network"] = _configuration.FlightSimulationNetwork,
            ["from"] = _configuration.StationIdentifier,
            ["to"] = to,
            ["type"] = messageType
        };

        if (!string.IsNullOrEmpty(packet))
            parameters["packet"] = packet;

        var content = new FormUrlEncodedContent(parameters);

        try
        {
            var response = await _httpClient.PostAsync(_configuration.Url, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Send response: {Response}", responseText);

            _lastSendTime = _clock.UtcNow();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send message");
            throw;
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            _logger.LogWarning("Send request timed out, skipping");
        }
    }

    public async Task Disconnect(CancellationToken cancellationToken)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HoppieAcarsClient));

        if (_pollCancellationTokenSource is not null)
            await _pollCancellationTokenSource.CancelAsync();
        
        if (_pollTask is not null)
            await _pollTask;
        
        _logger.LogInformation(
            "Disconnected {Station} on {Network} from Hoppies ACARS network",
            _configuration.StationIdentifier,
            _configuration.FlightSimulationNetwork);
    }

    async Task Poll(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var parameters = new Dictionary<string, string>
                    {
                        ["logon"] = _configuration.AuthenticationCode,
                        ["to"] = _configuration.StationIdentifier,
                        ["type"] = "poll"
                    };

                    var content = new FormUrlEncodedContent(parameters);
                    var response = await _httpClient.PostAsync(_configuration.Url, content, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (!string.IsNullOrWhiteSpace(responseText) && responseText != "ok")
                    {
                        ParseAndPublishDownlinkMessages(responseText);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Poll request failed");
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
                {
                    _logger.LogWarning("Poll request timed out, skipping this attempt");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception when polling messages");
                }

                var pollInterval = GetPollInterval();
                _logger.LogDebug("Poll completed, waiting {PollInterval}", pollInterval);
                await Task.Delay(pollInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Stopping poll task");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception when polling messages");
        }
    }

    void ParseAndPublishDownlinkMessages(string responseText)
    {
        try
        {
            var lines = responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("ok", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                {
                    _logger.LogWarning("Invalid message format: {Line}", line);
                    continue;
                }

                var from = parts[0];
                var to = parts[1];
                var type = parts[2];
                var packet = parts.Length > 3 ? string.Join(" ", parts[3..]) : string.Empty;

                var message = type.ToLowerInvariant() switch
                {
                    "telex" => new TelexDownlinkMessage(from, packet),
                    "cpdlc" => ParseCpdlcMessage(from, to, packet),
                    _ => null
                };

                if (message != null)
                {
                    _messageChannel.Writer.TryWrite(message);
                    _logger.LogDebug("Received {MessageType} from {From} to {To}", type, from, to);
                }
                else
                {
                    _logger.LogInformation("Unsupported message type: {Type}", type);
                }
            }
        }
        catch (Exception ex)
        {
            throw new MessageParseException("Failed to parse downlink messages", ex);
        }
    }

    static IAcarsMessage ParseCpdlcMessage(string from, string to, string packet)
    {
        var parts = packet.Split('/');
        if (parts.Length != 6)
            throw new Exception($"Invalid CPDLC packet: Expected 5 components, got {parts.Length}: \"{packet}\"");

        var messageId = int.Parse(parts[2]);
        int? replyToId = !string.IsNullOrEmpty(parts[3]) ? int.Parse(parts[3]) : null;
        var responseType = parts[4] switch
        {
            "NE" => CpdlcResponseType.NoResponse,
            "WU" => CpdlcResponseType.WilcoUnable,
            "AN" => CpdlcResponseType.AffirmativeNegative,
            "R" => CpdlcResponseType.Roger,
            _ => throw new ArgumentOutOfRangeException($"Unexpected CPDLC response type: {parts[4]}")
        };

        var content = parts[5];

        if (replyToId is not null)
        {
            return new CpdlcDownlinkReply(messageId, from, replyToId.Value, new CpdlcMessage(content, responseType));
        }
        else
        {
            return new CpdlcDownlinkMessage(messageId, from, new CpdlcMessage(content, responseType));
        }
    }

    TimeSpan GetPollInterval()
    {
        if (_lastSendTime.HasValue)
        {
            var timeSinceLastSend = _clock.UtcNow() - _lastSendTime.Value;
            if (timeSinceLastSend < TimeSpan.FromMinutes(1))
            {
                return TimeSpan.FromSeconds(20);
            }
        }

        return TimeSpan.FromSeconds(_random.Next(45, 76));
    }

    static string GetMessageType(IAcarsMessage message) => message switch
    {
        ITelexMessage => "telex",
        ICpdlcMessage => "cpdlc",
        _ => throw new ArgumentException($"Unexpected message type: {message.GetType().Name}")
    };

    static string GetTo(IAcarsMessage message) => message switch
    {
        IUplinkMessage m => m.Recipient,
        _ => throw new ArgumentException($"Unexpected message type: {message.GetType().Name}")
    };

    string GetPacket(IAcarsMessage message) => message switch
    {
        ITelexMessage m => UrlEncoder.Default.Encode(m.Content),
        ICpdlcMessage m => SerializeCpdlcMessage(m),
        _ => throw new ArgumentException($"Unexpected message type: {message.GetType().Name}")
    };

    int _messageId = 0;
    
    string SerializeCpdlcMessage(ICpdlcMessage cpdlcMessage)
    {
        var responseType = cpdlcMessage.Message.ResponseType switch
        {
            CpdlcResponseType.NoResponse => "NE",
            CpdlcResponseType.WilcoUnable => "WU",
            CpdlcResponseType.AffirmativeNegative => "AN",
            CpdlcResponseType.Roger => "R",
            _ => throw new ArgumentException($"Unexpected CpdlcResponseType: {cpdlcMessage.Message.ResponseType}")
        };

        var messageId = Interlocked.Increment(ref _messageId);

        var replyToId = cpdlcMessage is ICpdlcReplyMessage cpdlcReplyMessage
            ? cpdlcReplyMessage.ReplyToMessageId.ToString()
            : string.Empty;
        
        return $"/data/{messageId}/{replyToId}/{responseType}/{UrlEncoder.Default.Encode(cpdlcMessage.Message.Content)}";
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;
        
        await Disconnect(CancellationToken.None);
        
        if (_pollTask is not null)
            _pollTask.Dispose();
        
        if (_pollCancellationTokenSource is not null)
            _pollCancellationTokenSource.Dispose();
        
        _httpClient.Dispose();
        
        _disposed = true;
    }
}