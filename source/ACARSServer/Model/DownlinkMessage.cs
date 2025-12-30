using ACARSServer.Contracts;

namespace ACARSServer.Model;

public class DownlinkMessage(
    int messageId,
    int? messageReference,
    string sender,
    CpdlcDownlinkResponseType responseType,
    AlertType alertType,
    string content,
    DateTimeOffset received)
    : ICpdlcMessage
{
    public int MessageId { get; } = messageId;
    public int? MessageReference { get; } = messageReference;
    public string Sender { get; } = sender;
    public CpdlcDownlinkResponseType ResponseType { get; } = responseType;
    public AlertType AlertType { get; } = alertType;
    public string Content { get; } = content;
    public DateTimeOffset Received { get; } = received;
    public bool IsClosed { get; private set; } = responseType == CpdlcDownlinkResponseType.NoResponse; // Downlink messages requiring no response are self-closing

    public bool IsAcknowledged { get; set; }
    public bool IsControllerLate { get; set; }

    DateTimeOffset ICpdlcMessage.Time => Received;
    
    public void Close()
    {
        IsClosed = true;
    }
}
