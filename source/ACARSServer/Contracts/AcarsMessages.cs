using ACARSServer.Model;
using Newtonsoft.Json;

namespace ACARSServer.Contracts;

public interface IUplinkMessage
{
    string Recipient { get; }
}

public interface IDownlinkMessage
{
    string Sender { get; }
}

public enum CpdlcDownlinkResponseType
{
    NoResponse,
    ResponseRequired
}

public enum CpdlcUplinkResponseType
{
    NoResponse,
    WilcoUnable,
    AffirmativeNegative,
    Roger
}

public interface ICpdlcMessageDto
{
    int Id { get; }
}

public record ConnectedAircraftInfo(
    string Callsign,
    string StationId,
    string FlightSimulationNetwork,
    DataAuthorityState DataAuthorityState);

// Dialogue DTOs for SignalR API

public record DialogueDto(
    Guid Id,
    string AircraftCallsign,
    IReadOnlyList<CpdlcMessageDto> Messages,
    DateTimeOffset Opened,
    DateTimeOffset? Closed,
    DateTimeOffset? Archived);

[JsonObject(MemberSerialization.OptIn)]
public abstract class CpdlcMessageDto
{
    [JsonProperty("$type")]
    public abstract string Type { get; }

    [JsonProperty]
    public required int MessageId { get; init; }

    [JsonProperty]
    public int? MessageReference { get; init; }

    [JsonProperty]
    public required AlertType AlertType { get; init; }

    [JsonProperty]
    public required DateTimeOffset Time { get; init; }

    [JsonProperty]
    public DateTimeOffset? Closed { get; init; }

    [JsonProperty]
    public DateTimeOffset? Acknowledged { get; init; }
}

public class UplinkMessageDto : CpdlcMessageDto
{
    public override string Type => "uplink";

    [JsonProperty]
    public required string Recipient { get; init; }

    [JsonProperty]
    public required string ResponseType { get; init; }

    [JsonProperty]
    public required string Content { get; init; }

    [JsonProperty]
    public required DateTimeOffset Sent { get; init; }

    [JsonProperty]
    public required bool IsPilotLate { get; init; }

    [JsonProperty]
    public required bool IsTransmissionFailed { get; init; }
}

public class DownlinkMessageDto : CpdlcMessageDto
{
    public override string Type => "downlink";

    [JsonProperty]
    public required string Sender { get; init; }

    [JsonProperty]
    public required string ResponseType { get; init; }

    [JsonProperty]
    public required string Content { get; init; }

    [JsonProperty]
    public required DateTimeOffset Received { get; init; }

    [JsonProperty]
    public required bool IsControllerLate { get; init; }
}

// Converter for Dialogue to DialogueDto

public static class DialogueConverter
{
    public static DialogueDto ToDto(Dialogue dialogue)
    {
        return new DialogueDto(
            dialogue.Id,
            dialogue.AircraftCallsign,
            dialogue.Messages.Select(x => ToMessageDto(x)).ToList(),
            dialogue.Opened,
            dialogue.Closed,
            dialogue.Archived);
    }

    static CpdlcMessageDto ToMessageDto(ICpdlcMessage message)
    {
        return message switch
        {
            UplinkMessage uplink => new UplinkMessageDto
            {
                MessageId = uplink.MessageId,
                MessageReference = uplink.MessageReference,
                AlertType = uplink.AlertType,
                Time = uplink.Sent,
                Closed = uplink.Closed,
                Acknowledged = uplink.Sent,
                Recipient = uplink.Recipient,
                ResponseType = uplink.ResponseType.ToString(),
                Content = uplink.Content,
                Sent = uplink.Sent,
                IsPilotLate = uplink.IsPilotLate,
                IsTransmissionFailed = uplink.IsTransmissionFailed
            },

            DownlinkMessage downlink => new DownlinkMessageDto
            {
                MessageId = downlink.MessageId,
                MessageReference = downlink.MessageReference,
                AlertType = downlink.AlertType,
                Time = downlink.Received,
                Closed = downlink.Closed,
                Acknowledged = downlink.Acknowledged,
                Sender = downlink.Sender,
                ResponseType = downlink.ResponseType.ToString(),
                Content = downlink.Content,
                Received = downlink.Received,
                IsControllerLate = downlink.IsControllerLate
            },

            _ => throw new ArgumentException($"Unknown message type: {message.GetType()}")
        };
    }
}
