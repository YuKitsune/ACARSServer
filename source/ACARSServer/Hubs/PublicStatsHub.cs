using ACARSServer.Services;
using Microsoft.AspNetCore.SignalR;


namespace ACARSServer.Hubs;

public class PublicStatsHub : Hub
{
    private readonly IStatisticsService _statisticsService;
    private readonly ILogger _logger;

    public PublicStatsHub(
        IStatisticsService statisticsService,
        ILogger logger)
    {
        _statisticsService = statisticsService;
        _logger = logger.ForContext<PublicStatsHub>();
    }

    public override async Task OnConnectedAsync()
    {
        _logger.Information("Public stats client connected: {ConnectionId}", Context.ConnectionId);

        // Send current statistics immediately when client connects
        var stats = _statisticsService.GetCurrentStatistics();
        await Clients.Caller.SendAsync("UpdateStatistics", stats);

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.Information("Public stats client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
