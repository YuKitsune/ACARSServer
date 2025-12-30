using ACARSServer.Extensions;
using ACARSServer.Model;

namespace ACARSServer.Persistence;

public class InMemoryDialogueRepository : IDialogueRepository
{
    readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly List<Dialogue> _dialogues = new();

    public async Task Add(Dialogue dialogue, CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            _dialogues.Add(dialogue);
        }
    }

    public async Task<Dialogue?> FindDialogueForMessage(
        string flightSimulationNetwork,
        string stationIdentifier,
        string aircraftCallsign,
        int messageId,
        CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _dialogues
                .FirstOrDefault(d =>
                    d.FlightSimulationNetwork == flightSimulationNetwork &&
                    d.StationIdentifier == stationIdentifier &&
                    d.AircraftCallsign == aircraftCallsign &&
                    d.Messages.Any(m => m.MessageId == messageId));
        }
    }

    public async Task<Dialogue[]> All(CancellationToken cancellationToken)
    {
        using (await _semaphore.LockAsync(cancellationToken))
        {
            return _dialogues.ToArray();
        }
    }
}
