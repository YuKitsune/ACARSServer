using ACARSServer.Clients;

namespace ACARSServer.Contracts;

public interface IAcarsMessage;

public interface IUplinkMessage
{
    string Recipient { get; }
}

public interface ITelexMessage : IAcarsMessage
{
    public string Content { get; }
}

public record TelexUplinkMessage(string Recipient, string Content) : ITelexMessage, IUplinkMessage;
public record TelexDownlinkMessage(string Sender, string Content) : ITelexMessage;

public enum CpdlcResponseType
{
    NoResponse,
    WilcoUnable,
    AffirmativeNegative,
    Roger
}

public interface ICpdlcMessage : IAcarsMessage
{
    public CpdlcMessage Message { get; }
}

public interface ICpdlcReplyMessage : ICpdlcMessage
{
    public int ReplyToMessageId { get; }
}

public record CpdlcUplinkMessage(int MessageId, string Recipient, CpdlcMessage Message) : ICpdlcMessage, IUplinkMessage;
public record CpdlcUplinkReply(int MessageId, string Recipient, int ReplyToMessageId, CpdlcMessage Message) : ICpdlcMessage, ICpdlcReplyMessage, IUplinkMessage;

public record CpdlcDownlinkMessage(int MessageId, string Sender, CpdlcMessage Message) : ICpdlcMessage;
public record CpdlcDownlinkReply(int MessageId, string Sender, int ReplyToMessageId, CpdlcMessage Message) : ICpdlcMessage, ICpdlcReplyMessage;

public record CpdlcMessage(string Content, CpdlcResponseType ResponseType);
