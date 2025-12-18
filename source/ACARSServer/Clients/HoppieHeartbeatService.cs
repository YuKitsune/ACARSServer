namespace ACARSServer.Clients;

public class HoppieHeartbeatService : BackgroundService
{
    readonly HoppiesConfiguration[] _hoppieConfigurations;
    readonly ILogger<HoppieHeartbeatService> _logger;

    public HoppieHeartbeatService(
        IConfiguration configuration,
        ILogger<HoppieHeartbeatService> logger)
    {
        var hoppiesConfigurationSection = configuration.GetSection("Hoppies");
        _hoppieConfigurations = hoppiesConfigurationSection.Get<HoppiesConfiguration[]>() ?? [];
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Hoppie heartbeat service for {Count} configurations", _hoppieConfigurations.Length);

        var heartbeatTasks = _hoppieConfigurations
            .Select(config => SendHeartbeats(config, stoppingToken))
            .ToArray();

        await Task.WhenAll(heartbeatTasks);
    }

    async Task SendHeartbeats(HoppiesConfiguration configuration, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient();
        httpClient.BaseAddress = configuration.Url;
        httpClient.Timeout = TimeSpan.FromSeconds(15);

        var heartbeatInterval = TimeSpan.FromMinutes(configuration.HeartbeatIntervalMinutes);

        _logger.LogInformation(
            "Starting heartbeat for {Network}/{StationId} with interval {Interval}",
            configuration.FlightSimulationNetwork,
            configuration.StationIdentifier,
            heartbeatInterval);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Sending heartbeat ping for {Network}/{StationId}", configuration.FlightSimulationNetwork, configuration.StationIdentifier);

                try
                {
                    var parameters = new Dictionary<string, string>
                    {
                        ["logon"] = configuration.AuthenticationCode,
                        ["network"] = configuration.FlightSimulationNetwork,
                        ["from"] = configuration.StationIdentifier,
                        ["to"] = "SERVER",
                        ["type"] = "ping"
                    };

                    var content = new FormUrlEncodedContent(parameters);
                    var response = await httpClient.PostAsync(configuration.Url, content, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (!responseText.Equals("ok", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "Heartbeat ping for {Network}/{StationId} returned unexpected response: {Response}",
                            configuration.FlightSimulationNetwork,
                            configuration.StationIdentifier,
                            responseText);
                    }
                    else
                    {
                        _logger.LogInformation("Heartbeat ping for {Network}/{StationId} completed successfully", configuration.FlightSimulationNetwork, configuration.StationIdentifier);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Heartbeat ping request failed for {Network}/{StationId}", configuration.FlightSimulationNetwork, configuration.StationIdentifier);
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
                {
                    _logger.LogWarning("Heartbeat ping request timed out for {Network}/{StationId}", configuration.FlightSimulationNetwork, configuration.StationIdentifier);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception during heartbeat ping for {Network}/{StationId}", configuration.FlightSimulationNetwork, configuration.StationIdentifier);
                }

                await Task.Delay(heartbeatInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Stopping heartbeat for {Network}/{StationId}", configuration.FlightSimulationNetwork, configuration.StationIdentifier);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception in heartbeat task for {Network}/{StationId}", configuration.FlightSimulationNetwork, configuration.StationIdentifier);
        }
    }
}
