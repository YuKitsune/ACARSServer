namespace ACARSServer.Model;

// TODO: Separate formatted and plaintext contents.

public class UplinkMessage(
    int messageId,
    int? messageReference,
    string recipient,
    CpdlcUplinkResponseType responseType,
    AlertType alertType,
    string content,
    DateTimeOffset sent)
    : ICpdlcMessage
{
    public int MessageId { get; } = messageId;
    public int? MessageReference { get; } = messageReference;
    public string Recipient { get; } = recipient;
    public CpdlcUplinkResponseType ResponseType { get; } = responseType;
    public AlertType AlertType { get; } = alertType;
    public string Content { get; } = content;
    public DateTimeOffset Sent { get; } = sent;
    public bool IsClosed { get; private set; } = responseType == CpdlcUplinkResponseType.NoResponse; // Uplink messages requiring no response are self-closing
    public bool ClosedManually { get; private set; }
    
    public bool IsAcknowledged { get; set; }
    // public bool CanAction { get; set; }
    // public bool Actioned { get; set; }
    public bool IsPilotLate { get; set; }
    public bool IsTransmissionFailed { get; set; }

    DateTimeOffset ICpdlcMessage.Time => Sent;
    
    public void Close(bool manual = false)
    {
        IsClosed = true;
        ClosedManually = manual;
    }

    void ICpdlcMessage.Close() => Close();
}