using ACARSServer.Hubs;
using ACARSServer.Messages;
using ACARSServer.Services;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace ACARSServer.Handlers;

public class StatisticsBroadcastNotificationHandler(
    IHubContext<PublicStatsHub> hubContext,
    IStatisticsService statisticsService,
    ILogger<StatisticsBroadcastNotificationHandler> logger)
    : INotificationHandler<ControllerConnectedNotification>, 
        INotificationHandler<ControllerDisconnectedNotification>
{
    public async Task Handle(ControllerConnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Broadcasting statistics update (controller connected)");
        await BroadcastStatistics();
    }

    public async Task Handle(ControllerDisconnectedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogDebug("Broadcasting statistics update (controller disconnected)");
        await BroadcastStatistics();
    }

    private async Task BroadcastStatistics()
    {
        var stats = statisticsService.GetCurrentStatistics();
        await hubContext.Clients.All.SendAsync("UpdateStatistics", stats);
    }
}
