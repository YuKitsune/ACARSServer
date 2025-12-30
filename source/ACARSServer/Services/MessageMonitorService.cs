using ACARSServer.Infrastructure;
using ACARSServer.Messages;
using ACARSServer.Model;
using ACARSServer.Persistence;
using MediatR;

namespace ACARSServer.Services;

public class MessageMonitorService(IDialogueRepository repository, IClock clock, IPublisher publisher, ILogger logger)
    : BackgroundService
{
    readonly TimeSpan _timeoutCheckInterval = TimeSpan.FromSeconds(5);
    
    readonly TimeSpan _pilotLateTimeout = TimeSpan.FromMinutes(2);
    readonly TimeSpan _controllerLateTimeout = TimeSpan.FromMinutes(2);
    private readonly TimeSpan _archiveDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.Information("Starting message monitor service");
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    logger.Debug("Running message monitor iteration");
                    await CheckForTimeouts(stoppingToken);
                    await ArchiveCompletedDialogues(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    logger.Debug("Message monitor task cancellation requested");
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error during message monitoring iteration");
                }
                finally
                {
                    await Task.Delay(_timeoutCheckInterval, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.Information("Message monitor task stopped");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Fatal error in message monitor task");
        }
    }

    internal async Task CheckForTimeouts(CancellationToken cancellationToken)
    {
        // TODO: Verify if this is supposed to apply to message closures or acknowledgements.

        var now = clock.UtcNow();
        var dialogues = await repository.All(cancellationToken);

        logger.Debug("Checking for timeouts in {DialogueCount} dialogues", dialogues.Length);

        foreach (var dialogue in dialogues)
        {
            if (dialogue.IsClosed)
                continue;
            
            var anyChanges = false;
            foreach (var message in dialogue.Messages)
            {
                switch (message)
                {
                    case UplinkMessage uplink:
                        if (CheckUplinkTimeout(uplink, now))
                        {
                            anyChanges = true;
                        }
                        break;

                    case DownlinkMessage downlink:
                        if (CheckDownlinkTimeout(downlink, now))
                        {
                            anyChanges = true;
                        }
                        break;
                }
            }

            if (anyChanges)
            {
                await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
            }
        }
    }

    internal async Task ArchiveCompletedDialogues(CancellationToken cancellationToken)
    {
        var now = clock.UtcNow();
        var dialogues = await repository.All(cancellationToken);

        logger.Debug("Checking for completed dialogues to archive");

        foreach (var dialogue in dialogues)
        {
            if (!dialogue.IsClosed)
                continue;

            var lastAckTime = dialogue.Messages.Max(d => d.Acknowledged);
            if (lastAckTime is null)
                continue;

            var archiveTime = lastAckTime.Value.Add(_archiveDelay);
            if (now < archiveTime)
                continue;

            logger.Debug("Archiving dialogue for {Callsign}", dialogue.AircraftCallsign);
            dialogue.Archive(now);

            await publisher.Publish(new DialogueChangedNotification(dialogue), cancellationToken);
        }
    }

    bool CheckUplinkTimeout(UplinkMessage uplink, DateTimeOffset now)
    {
        // Skip if already closed, in timeout/failed state, or doesn't require response
        if (uplink.IsClosed ||
            uplink.IsPilotLate ||
            uplink.IsTransmissionFailed ||
            uplink.ResponseType == CpdlcUplinkResponseType.NoResponse)
            return false;

        // Check if timeout has been exceeded
        var timeSinceSent = now - uplink.Sent;
        if (timeSinceSent < _pilotLateTimeout)
            return false;

        logger.Debug("Uplink message {UplinkId} marked as pilot late (time since sent: {TimeSinceSent}s)",
            uplink.MessageId, timeSinceSent.TotalSeconds);
        uplink.IsPilotLate = true;
        return true;
    }

    bool CheckDownlinkTimeout(DownlinkMessage downlink, DateTimeOffset now)
    {
        // Skip if already closed, in timeout state, or doesn't require response
        if (downlink.IsClosed ||
            downlink.IsControllerLate ||
            downlink.ResponseType == CpdlcDownlinkResponseType.NoResponse)
            return false;

        // Check if timeout has been exceeded
        var timeSinceReceived = now - downlink.Received;
        if (timeSinceReceived < _controllerLateTimeout)
            return false;

        logger.Debug("Downlink message {DownlinkId} marked as controller late (time since received: {TimeSinceReceived}s)",
            downlink.MessageId, timeSinceReceived.TotalSeconds);
        downlink.IsControllerLate = true;
        return true;
    }
}
