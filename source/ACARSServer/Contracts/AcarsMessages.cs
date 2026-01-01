using System.Text.Json.Serialization;
using ACARSServer.Model;

namespace ACARSServer.Contracts;

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

[JsonDerivedType(typeof(UplinkMessageDto), "uplink")]
[JsonDerivedType(typeof(DownlinkMessageDto), "downlink")]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
public abstract class CpdlcMessageDto
{
    public required int MessageId { get; init; }
    public int? MessageReference { get; init; }
    public required AlertType AlertType { get; init; }
    public abstract DateTimeOffset Time { get; }
    public DateTimeOffset? Closed { get; init; }
    public DateTimeOffset? Acknowledged { get; init; }
}

public class UplinkMessageDto : CpdlcMessageDto
{
    public override DateTimeOffset Time => Sent;
    public required string Recipient { get; init; }
    public required string SenderCallsign { get; init; }
    public required CpdlcUplinkResponseType ResponseType { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset Sent { get; init; }
    public required bool IsClosedManually { get; init; }
    public required bool IsPilotLate { get; init; }
    public required bool IsTransmissionFailed { get; init; }
}

public class DownlinkMessageDto : CpdlcMessageDto
{
    public override DateTimeOffset Time => Received;
    public required string Sender { get; init; }
    public required CpdlcDownlinkResponseType ResponseType { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset Received { get; init; }
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
            dialogue.Messages.Select(ToMessageDto).ToList(),
            dialogue.Opened,
            dialogue.Closed,
            dialogue.Archived);
    }

    public static CpdlcMessageDto ToMessageDto(ICpdlcMessage message)
    {
        return message switch
        {
            UplinkMessage uplink => ToDto(uplink),
            DownlinkMessage downlink => ToDto(downlink),
            _ => throw new ArgumentException($"Unknown message type: {message.GetType()}")
        };
    }

    public static UplinkMessageDto ToDto(UplinkMessage uplink)
    {
        return new UplinkMessageDto
        {
            MessageId = uplink.MessageId,
            MessageReference = uplink.MessageReference,
            AlertType = uplink.AlertType,
            Closed = uplink.Closed,
            IsClosedManually = uplink.ClosedManually,
            Acknowledged = uplink.Sent,
            Recipient = uplink.Recipient,
            SenderCallsign = uplink.SenderCallsign,
            ResponseType = uplink.ResponseType switch
            {
                Model.CpdlcUplinkResponseType.NoResponse => CpdlcUplinkResponseType.NoResponse,
                Model.CpdlcUplinkResponseType.WilcoUnable => CpdlcUplinkResponseType.WilcoUnable,
                Model.CpdlcUplinkResponseType.AffirmativeNegative => CpdlcUplinkResponseType.AffirmativeNegative,
                Model.CpdlcUplinkResponseType.Roger => CpdlcUplinkResponseType.Roger,
                _ => throw new ArgumentException($"Unknown uplink response type: {uplink.ResponseType}")
            },
            Content = uplink.Content,
            Sent = uplink.Sent,
            IsPilotLate = uplink.IsPilotLate,
            IsTransmissionFailed = uplink.IsTransmissionFailed
        };
    }

    public static DownlinkMessageDto ToDto(DownlinkMessage downlink)
    {
        return new DownlinkMessageDto
        {
            MessageId = downlink.MessageId,
            MessageReference = downlink.MessageReference,
            AlertType = downlink.AlertType,
            Closed = downlink.Closed,
            Acknowledged = downlink.Acknowledged,
            Sender = downlink.Sender,
            ResponseType = downlink.ResponseType switch
            {
                Model.CpdlcDownlinkResponseType.NoResponse => CpdlcDownlinkResponseType.NoResponse,
                Model.CpdlcDownlinkResponseType.ResponseRequired => CpdlcDownlinkResponseType.ResponseRequired,
                _ => throw new ArgumentException($"Unknown downlink response type: {downlink.ResponseType}")
            },
            Content = downlink.Content,
            Received = downlink.Received,
            IsControllerLate = downlink.IsControllerLate
        };
    }
}