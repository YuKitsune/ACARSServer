using System.Collections;
using ACARSServer.Model;

namespace ACARSServer.Persistence;

public interface IDialogueRepository
{
    Task Add(Dialogue dialogue, CancellationToken cancellationToken);
    
    Task<Dialogue?> FindDialogueForMessage(
        string flightSimulationNetwork,
        string stationIdentifier,
        string aircraftCallsign,
        int messageId,
        CancellationToken cancellationToken);

    Task<Dialogue[]> All(CancellationToken cancellationToken);
    Task Remove(Dialogue dialogue, CancellationToken cancellationToken);
}