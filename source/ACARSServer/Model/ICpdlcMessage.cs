namespace ACARSServer.Model;

public interface ICpdlcMessage
{
    public int MessageId { get; }
    public int? MessageReference { get; }
    AlertType AlertType { get; }
    DateTimeOffset Time { get; }
    bool IsClosed { get; }
    void Close();
}